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
using Microsoft.IdentityModel.TestUtils;
using Microsoft.IdentityModel.Xml;
using Xunit;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Microsoft.IdentityModel.Tokens.Xml.Tests
{
    public class EnvelopedSignatureReaderTests
    {
        [Fact]
        public void GetSets()
        {
            var type = typeof(EnvelopedSignatureReader);
            var properties = type.GetProperties();
            Assert.True(properties.Length == 35, $"Number of properties has changed from 35 to: {properties.Length}, adjust tests");

            var reader = XmlUtilities.CreateEnvelopedSignatureReader(Default.OuterXml);
            var defaultSerializer = reader.Serializer;
            var context = new GetSetContext
            {
                PropertyNamesAndSetGetValue = new List<KeyValuePair<string, List<object>>>
                {
                    new KeyValuePair<string, List<object>>("Serializer", new List<object>{ defaultSerializer, DSigSerializer.Default}),
                },
                Object = reader,
            };

            TestUtilities.GetSet(context);
            TestUtilities.AssertFailIfErrors($"{this}.GetSets", context.Errors);
        }

        [Theory, MemberData(nameof(ConstructorTheoryData))]
        public void Constructor(EnvelopedSignatureTheoryData theoryData)
        {
            TestUtilities.WriteHeader($"{this}.Constructor", theoryData);
            try
            {
                var envelopedReader = new EnvelopedSignatureReader(theoryData.XmlReader);
                while (envelopedReader.Read());

                if (theoryData.ExpectSignature)
                {
                    if (envelopedReader.Signature == null)
                        Assert.False(true, "theoryData.ExpectSignature == true && envelopedReader.ExpectSignature == null");

                    envelopedReader.Signature.Verify(theoryData.SecurityKey, theoryData.SecurityKey.CryptoProviderFactory);
                }

                theoryData.ExpectedException.ProcessNoException();
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex);
            }
        }

        public static TheoryData<EnvelopedSignatureTheoryData> ConstructorTheoryData
        {
            get
            {
                var theoryData = new TheoryData<EnvelopedSignatureTheoryData>();

                theoryData.Add(new EnvelopedSignatureTheoryData
                {
                    ExpectedException = ExpectedException.ArgumentNullException("IDX10000:"),
                    First = true,
                    TestId = "Null XmlReader",
                    XmlReader = null
                });

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadSignedXmlTheoryData))]
        public void ReadSignedXml(EnvelopedSignatureTheoryData theoryData)
        {
            TestUtilities.WriteHeader($"{this}.ReadSignedXml", theoryData);
            var context = new CompareContext($"{this}.ReadSignedXml : {theoryData.TestId}.");
            try
            {
                var envelopedReader = XmlUtilities.CreateEnvelopedSignatureReader(theoryData.Xml);
                while(envelopedReader.Read());

                if (theoryData.ExpectSignature)
                {
                    if (envelopedReader.Signature == null)
                        Assert.False(true, "theoryData.ExpectSignature == true && envelopedReader.Signature == null");

                    envelopedReader.Signature.Verify(theoryData.SecurityKey, theoryData.CryptoProviderFactory);
                }

                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<EnvelopedSignatureTheoryData> ReadSignedXmlTheoryData
        {
            get
            {
                return new TheoryData<EnvelopedSignatureTheoryData>
                {
                    new EnvelopedSignatureTheoryData
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("IDX10000:"),
                        First = true,
                        TestId = nameof(ReferenceXml.Saml2TokenValidSigned) + ":SecurityKey==null",
                        Xml = ReferenceXml.Saml2TokenValidSigned
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("IDX10000:"),
                        SecurityKey = KeyingMaterial.DefaultAADSigningKey,
                        CryptoProviderFactory = null,
                        TestId = nameof(ReferenceXml.Saml2TokenValidSigned) + ":CryptoProviderFactory==null",
                        Xml = ReferenceXml.Saml2TokenValidSigned
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        SecurityKey = KeyingMaterial.DefaultAADSigningKey,
                        TestId = nameof(ReferenceXml.Saml2TokenValidSigned),
                        Xml = ReferenceXml.Saml2TokenValidSigned
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX30019:"),
                        SecurityKey = KeyingMaterial.DefaultAADSigningKey,
                        TestId = nameof(ReferenceXml.Saml2TokenTwoSignatures),
                        Xml = ReferenceXml.Saml2TokenTwoSignatures
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlValidationException)),
                        SecurityKey = KeyingMaterial.DefaultAADSigningKey,
                        TestId = nameof(ReferenceXml.Saml2TokenValidSignatureNOTFormated),
                        Xml = ReferenceXml.Saml2TokenValidSignatureNOTFormated
                    },
                    new EnvelopedSignatureTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(XmlValidationException)),
                        SecurityKey = KeyingMaterial.DefaultAADSigningKey,
                        TestId = nameof(ReferenceXml.Saml2TokenValidFormated),
                        Xml = ReferenceXml.Saml2TokenValidFormated
                    }
                };
            }
        }
    }
}

#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
