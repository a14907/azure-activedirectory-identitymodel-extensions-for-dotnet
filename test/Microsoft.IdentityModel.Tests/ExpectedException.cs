//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.IdentityModel.Tests
{
    /// <summary>
    /// When a test case throws an exception, this class helps to determine if the exception is as expected.
    /// Really just a helper for wrapping things.
    /// </summary>
    public class ExpectedException
    {
        public ExpectedException(Type typeExpected = null, string substringExpected = null, Type innerTypeExpected = null, Dictionary<string, object> propertiesExpected = null)
        {
            IgnoreInnerException = false;
            InnerTypeExpected = innerTypeExpected;
            SubstringExpected = substringExpected;
            TypeExpected = typeExpected;
            if (propertiesExpected != null)
                PropertiesExpected = propertiesExpected;
        }

        public static bool DefaultVerbose { get; set; } = false;

        public static ExpectedException ArgumentException(string substringExpected = null, Type inner = null)
        {
            return new ExpectedException(typeof(ArgumentException), substringExpected, inner);
        }
        public static ExpectedException ArgumentOutOfRangeException(string substringExpected = null, Type inner = null)
        {
            return new ExpectedException(typeof(ArgumentOutOfRangeException), substringExpected, inner);
        }

        public static ExpectedException ArgumentNullException(string substringExpected = null, Type inner = null)
        {
            return new ExpectedException(typeof(ArgumentNullException), substringExpected, inner); 
        }

        public static ExpectedException CryptographicException(string substringExpected = null, Type inner = null)
        {
            return new ExpectedException(typeof(CryptographicException), substringExpected, inner);
        }

        public static ExpectedException SecurityTokenDecryptionFailedException(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenDecryptionFailedException), substringExpected, innerTypeExpected);
        }

        public static ExpectedException InvalidOperationException(string substringExpected = null, Type inner = null, string contains = null)
        {
            return new ExpectedException(typeof(InvalidOperationException), substringExpected, inner);
        }

        public static ExpectedException IOException(string substringExpected = null, Type inner = null, string contains = null)
        {
            return new ExpectedException(typeof(IOException), substringExpected, inner);
        }

        public static ExpectedException NoExceptionExpected 
        { 
            get { return new ExpectedException(); }
        }

        public static ExpectedException NotSupportedException(string substringExpected = null, Type inner = null, string contains = null)
        {
            return new ExpectedException(typeof(NotSupportedException), substringExpected, inner);
        }

        public static ExpectedException ObjectDisposedException 
        { 
            get 
            {
                return new ExpectedException(typeof(ObjectDisposedException)); 
            } 
        }

        public void ProcessException(Exception exception, CompareContext context)
        {
            ProcessException(exception, context.Diffs);
        }

        public void ProcessException(Exception exception, List<string> errors = null)
        {
            if (TypeExpected == null && InnerTypeExpected != null)
            {
                HandleError("(TypeExpected == null && InnerTypeExpected != null. TypeExpected == null && InnerTypeExpected != null.", errors);
                return;
            }

            if (TypeExpected == null)
            {
                HandleError("exception != null, expectedException.TypeExpected == null.\nexception: " + exception, errors);
                return;
            }

            if (exception == null)
            {
                HandleError("exception == null, expectedException.TypeExpected != null.\nexpectedException.TypeExpected: " + TypeExpected, errors);
                return;
            }

            if (exception.GetType() != TypeExpected)
            {
                HandleError("exception.GetType() != expectedException.TypeExpected:\nexception.GetType(): " + exception.GetType() + "\nexpectedException.TypeExpected: " + TypeExpected, errors);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SubstringExpected) && !exception.Message.Contains(SubstringExpected))
            {
                HandleError("!exception.Message.Contains(SubstringExpected).\nexception.Message: " + exception.Message + "\nexpectedException.SubstringExpected: " + SubstringExpected, errors);
                return;
            }

            if (exception.InnerException != null && InnerTypeExpected == null)
            {
                HandleError("exception.InnerException != null && expectedException.InnerTypeExpected == null.\nexception.InnerException: " + exception.InnerException, errors);
                return;
            }

            if (exception.InnerException == null && InnerTypeExpected != null)
            {
                HandleError("exception.InnerException == null, expectedException.InnerTypeExpected != null.\nexpectedException.InnerTypeExpected: " + InnerTypeExpected, errors);
                return;
            }

            if ((InnerTypeExpected != null) && (exception.InnerException.GetType() != InnerTypeExpected ) && !IgnoreInnerException)
            {
                HandleError("exception.InnerException != expectedException.InnerTypeExpected." + "\nexception.InnerException: '" + exception.InnerException + "\nInnerTypeExpected: " + InnerTypeExpected, errors);
            }

            if (PropertiesExpected != null && PropertiesExpected.Count > 0)
            { 
                foreach (KeyValuePair<string, object> property in PropertiesExpected)
                {
                    PropertyInfo propertyInfo = TypeExpected.GetProperty(property.Key);
                    if (propertyInfo == null)
                    {
                        HandleError("exception type " + TypeExpected + " does not have expected property " + property.Key, errors);
                    }
                    object runtimeValue = propertyInfo.GetValue(exception);

                    bool expectedTypeIsNullable = propertyInfo.PropertyType.GetTypeInfo().IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    Type expectedTypeNonNullable = expectedTypeIsNullable ? propertyInfo.PropertyType.GetGenericArguments()[0] : propertyInfo.PropertyType;

                    if (runtimeValue != null && runtimeValue.GetType() != expectedTypeNonNullable && !expectedTypeNonNullable.IsAssignableFrom(runtimeValue.GetType()))
                    {
                        HandleError("exception type " + TypeExpected + " does not match the expected property " + property.Key + " type.\nexpected type: " + expectedTypeNonNullable + ", actual type: " + runtimeValue.GetType(), errors);
                    }

                    if (runtimeValue != property.Value && 
                        ((runtimeValue != null && !runtimeValue.Equals(property.Value)) ||
                         (property.Value != null && !property.Value.Equals(runtimeValue))))
                    {
                        HandleError("exception type " + TypeExpected + " doesn't have the expected property value " + property.Key + " value.\nexpected value: " + property.Value + ", actual value: " + runtimeValue, errors);
                    }
                }
            }

            if (DefaultVerbose || Verbose)
                Console.WriteLine(Environment.NewLine + "Exception displayed to user: " + Environment.NewLine + Environment.NewLine + exception);
        }

        public void ProcessNoException(List<string> errors = null)
        {
            if (TypeExpected != null)
            {
                if (errors != null)
                    errors.Add("Inside ProcessNoException: expectedException.TypeExpected != null: " + TypeExpected);
                else
                    throw new TestException("Inside ProcessNoException: expectedException.TypeExpected != null: '" + TypeExpected);
            }
        }

        public void ProcessNoException(CompareContext context)
        {
            if (TypeExpected != null)
                context.Diffs.Add("expectedException.TypeExpected != null: " + TypeExpected);
        }

        private static void HandleError(string error, List<string> errors )
        {
            if (errors != null)
                errors.Add(error);
            else
                throw new TestException($"List<string> errors == null, error in test: {error}.");
        }

        public static ExpectedException SecurityTokenEncryptionKeyNotFoundException(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenEncryptionKeyNotFoundException), substringExpected, innerTypeExpected);
        }

        public static ExpectedException SecurityTokenEncryptionFailedException(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenEncryptionFailedException), substringExpected, innerTypeExpected);
        }

        public static ExpectedException SecurityTokenException(string substringExpected = null, Type innertypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenException), substringExpected, innertypeExpected);
        }

        public static ExpectedException SecurityTokenExpiredException(string substringExpected = null, Type innerTypeExpected = null, Dictionary<string, object> propertiesExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenExpiredException), substringExpected, innerTypeExpected, propertiesExpected: propertiesExpected);
        }

        public static ExpectedException SecurityTokenInvalidAudienceException(string substringExpected = null, Type innerTypeExpected = null, Dictionary<string, object> propertiesExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenInvalidAudienceException), substringExpected, innerTypeExpected, propertiesExpected: propertiesExpected);
        }

        public static ExpectedException SecurityTokenInvalidIssuerException(string substringExpected = null, Type innerTypeExpected = null, Dictionary<string, object> propertiesExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenInvalidIssuerException), substringExpected, innerTypeExpected, propertiesExpected: propertiesExpected);
        }

        public static ExpectedException SecurityTokenInvalidLifetimeException(string substringExpected = null, Type innerTypeExpected = null, Dictionary<string, object> propertiesExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenInvalidLifetimeException), substringExpected, innerTypeExpected, propertiesExpected: propertiesExpected);
        }

        public static ExpectedException SecurityTokenInvalidSignatureException(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenInvalidSignatureException), substringExpected, innerTypeExpected);
        }

        public static ExpectedException SecurityTokenNoExpirationException(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenNoExpirationException), substringExpected, innerTypeExpected);
        }                

        public static ExpectedException SecurityTokenNotYetValidException(string substringExpected = null, Type innerTypeExpected = null, Dictionary<string, object> propertiesExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenNotYetValidException), substringExpected, innerTypeExpected, propertiesExpected: propertiesExpected);
        }

        public static ExpectedException SecurityTokenReplayAddFailed(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenReplayAddFailedException), substringExpected, innerTypeExpected);
        }

        public static ExpectedException SecurityTokenReplayDetected(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenReplayDetectedException), substringExpected, innerTypeExpected);
        }                

        public static ExpectedException SecurityTokenSignatureKeyNotFoundException(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenSignatureKeyNotFoundException), substringExpected, innerTypeExpected);
        }

        public static ExpectedException SecurityTokenValidationException(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenValidationException), substringExpected, innerTypeExpected);
        }

        public static ExpectedException SecurityTokenInvalidSigningKeyException(string substringExpected = null, Type innerTypeExpected = null, Dictionary<string, object> propertiesExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenInvalidSigningKeyException), substringExpected, innerTypeExpected, propertiesExpected: propertiesExpected);
        }

        public static ExpectedException KeyWrapException(string substringExpected = null, Type innerTypeExpected = null)
        {
            return new ExpectedException(typeof(SecurityTokenKeyWrapException), substringExpected, innerTypeExpected);
        }

        public bool IgnoreInnerException { get; set; }

        public Type InnerTypeExpected { get; set; }

        public Dictionary<string, object> PropertiesExpected { get; set; } = new Dictionary<string, object>();

        public string SubstringExpected { get; set; }

        public override string ToString()
        {
            if (TypeExpected == null)
                return $"NoExceptionExpected";
            else
                return $"{TypeExpected}, Substring: {SubstringExpected}";
        }

        public Type TypeExpected { get; set; }

        public bool Verbose { get; set; } = false;
    }
}