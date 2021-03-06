﻿using ComLight.Emit;
using ComLight.Marshalling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ComLight
{
	public static partial class ManagedWrapper
	{
		/// <summary>Expression visitor which replaces a specific ParameterExpression with a ConstantExpression</summary>
		class ParamReplacementVisitor: ExpressionVisitor
		{
			readonly ParameterExpression replacing;
			readonly ConstantExpression replacement;

			public ParamReplacementVisitor( ParameterExpression replacing, ConstantExpression replacement )
			{
				this.replacing = replacing;
				this.replacement = replacement;
			}

			protected override Expression VisitParameter( ParameterExpression node )
			{
				if( node == replacing )
					return replacement;
				else
					return base.VisitParameter( node );
			}
		}

		/// <summary>Expression tree prefab for a single interface method.</summary>
		class MethodBuilder
		{
			/// <summary>The first one is always ManagedWrapper.paramNativeObject, the rest of them vary depending on what was in the interface.</summary>
			readonly ParameterExpression[] nativeParameters;
			/// <summary>It has one extra parameter, "managed", replaced when compiling.</summary>
			readonly BlockExpression expression;
			/// <summary>Native function delegate it builds</summary>
			readonly Type tDelegate;

			/// <summary>Build the prefab</summary>
			public MethodBuilder( ParameterExpression managed, MethodInfo mi, Type tDelegate )
			{
				if( mi.hasRetValIndex() )
					throw new NotImplementedException( "So far, [RetValIndex] attribute is only implemented for C++ objects." );

				this.tDelegate = tDelegate;

				ParameterInfo[] parameters = mi.GetParameters();
				nativeParameters = new ParameterExpression[ parameters.Length + 1 ];
				nativeParameters[ 0 ] = paramNativeObject;

				Expression[] managedParameters = new Expression[ parameters.Length ];
				List<ParameterExpression> localVars = new List<ParameterExpression>();
				List<Expression> block = new List<Expression>();
				for( int i = 0; i < parameters.Length; i++ )
				{
					var pi = parameters[ i ];
					Type tp = pi.ParameterType;

					var cm = pi.customMarshaller();
					if( null != cm )
						tp = cm.getNativeType( pi );
					ParameterExpression pNative = Expression.Parameter( tp, pi.Name );
					Expressions custom = cm?.managed( pNative, !pi.IsOut );

					nativeParameters[ i + 1 ] = pNative;
					if( null != custom )
					{
						managedParameters[ i ] = custom.argument;
						if( null != custom.variable )
							localVars.Add( custom.variable );
						if( null != custom.after )
							block.Add( custom.after );
					}
					else
						managedParameters[ i ] = pNative;
				}

				Expression eCall = Expression.Call( managed, mi, managedParameters );

				if( block.Count <= 0 )
				{
					// No custom argument marshalers defined post-processing expressions.
					// This simplified a few things, also makes generated code slightly more efficient, one less local variable on the stack, and couple less instructions.

					if( mi.ReturnType != typeof( IntPtr ) )
					{
						Expression defaultReturnValue = MiscUtils.E_UNEXPECTED;

						if( mi.ReturnType == typeof( int ) )
							eCall = Expression.Return( returnTarget, eCall );
						else if( mi.ReturnType == typeof( bool ) )
							eCall = Expression.Return( returnTarget, Expression.Condition( eCall, MiscUtils.S_OK, MiscUtils.S_FALSE ) );
						else if( mi.ReturnType == typeof( void ) )
							defaultReturnValue = MiscUtils.S_OK;
						else
							throw new ArgumentException( $"Method { mi.DeclaringType.FullName }.{ mi.Name } has unsupported return type { mi.ReturnType.FullName }" );

						Expression eTryCatch = Expression.TryCatch( eCall, exprCatchBlock );
						block.Insert( 0, eTryCatch );

						Expression eReturnLabel = Expression.Label( returnTarget, defaultReturnValue );
						block.Add( eReturnLabel );

						expression = Expression.Block( typeof( int ), localVars, block );
					}
					else
					{
						eCall = Expression.Return( pointerReturnTarget, eCall );
						Expression eTryCatch = Expression.TryCatch( eCall, exprPointerCatchBlock );
						block.Insert( 0, eTryCatch );
						block.Add( Expression.Label( pointerReturnTarget, MiscUtils.nullptr ) );
						expression = Expression.Block( typeof( IntPtr ), localVars, block );
					}
				}
				else if( mi.ReturnType == typeof( void ) )
				{
					// Insert managedMethod(...) at the start of the block
					block.Insert( 0, eCall );
					Expression eTryBody = Expression.Block( typeof( void ), block );
					// Wrap into try-catch
					Expression eTryCatch = Expression.TryCatch( eTryBody, exprCatchBlock );

					// After the try-catch, return S_OK
					block.Clear();
					block.Add( eTryCatch );
					block.Add( Expression.Label( returnTarget, MiscUtils.S_OK ) );

					expression = Expression.Block( typeof( int ), localVars, block );
				}
				else if( mi.ReturnType == typeof( int ) )
				{
					// Insert `int hr = managedMethod(..)` at the start of the block
					ParameterExpression varHr = Expression.Variable( typeof( int ), "hr" );
					localVars.Add( varHr );
					block.Insert( 0, Expression.Assign( varHr, eCall ) );
					// Wrap into try-catch
					Expression eTryBody = Expression.Block( typeof( void ), block );
					Expression eTryCatch = Expression.TryCatch( eTryBody, exprCatchBlock );

					// After the try-catch, return hr
					block.Clear();
					block.Add( eTryCatch );
					block.Add( Expression.Label( returnTarget, varHr ) );

					expression = Expression.Block( typeof( int ), localVars, block );
				}
				else if( mi.ReturnType == typeof( bool ) )
				{
					// Insert `bool result = managedMethod(..)` at the start of the block
					ParameterExpression varResult = Expression.Variable( typeof( bool ), "result" );
					localVars.Add( varResult );
					block.Insert( 0, Expression.Assign( varResult, eCall ) );
					// Wrap into try-catch
					Expression eTryBody = Expression.Block( typeof( void ), block );
					Expression eTryCatch = Expression.TryCatch( eTryBody, exprCatchBlock );

					// After the try-catch, return `result ? S_OK : S_FALSE`
					block.Clear();
					block.Add( eTryCatch );
					var eCondition = Expression.Condition( varResult, MiscUtils.S_OK, MiscUtils.S_FALSE );
					block.Add( Expression.Label( returnTarget, eCondition ) );

					expression = Expression.Block( typeof( int ), localVars, block );
				}
				else if( mi.ReturnType == typeof( IntPtr ) )
				{
					// insert `IntPtr result = managedMethod(..)` at the start of the block
					ParameterExpression varResult = Expression.Variable( typeof( IntPtr ), "result" );
					localVars.Add( varResult );
					block.Insert( 0, Expression.Assign( varResult, eCall ) );
					// Wrap into try-catch
					Expression eTryBody = Expression.Block( typeof( void ), block );
					Expression eTryCatch = Expression.TryCatch( eTryBody, exprPointerCatchBlock );

					// After the try-catch, return `result`
					block.Clear();
					block.Add( eTryCatch );
					block.Add( Expression.Label( pointerReturnTarget, varResult ) );

					expression = Expression.Block( typeof( IntPtr ), localVars, block );
				}
				else
					throw new ArgumentException( $"Method { mi.DeclaringType.FullName }.{ mi.Name } has unsupported return type { mi.ReturnType.FullName }" );
			}

			/// <summary>Apply the expression tree visitor finalizing the prefab, and compile into lambda.</summary>
			public Delegate compile( ExpressionVisitor visitor )
			{
				Expression eBody = visitor.Visit( expression );
				LambdaExpression lambda = Expression.Lambda( tDelegate, eBody, nativeParameters );
				return lambda.Compile();
			}
		}

		/// <summary>`IntPtr pNative` argument</summary>
		static readonly ParameterExpression paramNativeObject;
		/// <summary>Return label with int type</summary>
		static readonly LabelTarget returnTarget;
		/// <summary>Return label with IntPtr type</summary>
		static readonly LabelTarget pointerReturnTarget;

		/// <summary>Catch block that returns <see cref="Exception.HResult" /> and jumps to <see cref="returnTarget" />.</summary>
		static readonly CatchBlock exprCatchBlock;

		/// <summary>Catch block that returns nullptr and jumps to <see cref="pointerReturnTarget" />.</summary>
		static readonly CatchBlock exprPointerCatchBlock;

		static ManagedWrapper()
		{
			// Create sub-expressions which don't depend on the interface type. This saves a bit of resources.
			paramNativeObject = Expression.Parameter( typeof( IntPtr ), "pNative" );

			Type tException = typeof( Exception );
			MethodInfo miExceptionHresult = tException.GetProperty( "HResult" ).GetGetMethod();

			// Create return target and catch block, they don't depend on the interface nor the input object
			returnTarget = Expression.Label( typeof( int ) );
			var eException = Expression.Parameter( typeof( Exception ), "ex" );
			var eCatchBody = Expression.Return( returnTarget, Expression.Property( eException, miExceptionHresult ) );
			exprCatchBlock = Expression.Catch( eException, eCatchBody );

			// Same for pointer-returning methods
			pointerReturnTarget = Expression.Label( typeof( IntPtr ) );
			eCatchBody = Expression.Return( pointerReturnTarget, MiscUtils.nullptr );
			exprPointerCatchBlock = Expression.Catch( eException, eCatchBody );
		}

		/// <summary>Expression tree prefabs for the complete COM interface</summary>
		class InterfaceBuilder
		{
			readonly Type tInterface;
			readonly ParameterExpression paramManagedObject;
			readonly MethodBuilder[] builders;

			/// <summary>Use reflection to build the prefab</summary>
			public InterfaceBuilder( Type tInterface )
			{
				this.tInterface = tInterface;

				paramManagedObject = Expression.Parameter( tInterface, "managed" );

				MethodInfo[] methods = tInterface.getMethodsWithoutProperties().ToArray();
				Type[] tDelegates = NativeDelegates.buildDelegates( tInterface );
				Debug.Assert( methods.Length == tDelegates.Length );

				builders = new MethodBuilder[ methods.Length ];
				for( int i = 0; i < methods.Length; i++ )
					builders[ i ] = new MethodBuilder( paramManagedObject, methods[ i ], tDelegates[ i ] );
			}

			/// <summary>Compile prefab into array of delegates. Delegate are of different types, each type has [UnmanagedFunctionPointer], and is compatible with the corresponding C++ interface method.</summary>
			public Delegate[] compile( object obj )
			{
				var managed = Expression.Constant( obj, tInterface );
				var visitor = new ParamReplacementVisitor( paramManagedObject, managed );

				Delegate[] results = new Delegate[ builders.Length ];
				for( int i = 0; i < results.Length; i++ )
					results[ i ] = builders[ i ].compile( visitor );
				return results;
			}
		}
	}
}