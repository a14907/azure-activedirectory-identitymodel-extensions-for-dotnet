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
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.TestUtils;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Tokens.Saml2;
using Xunit;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Microsoft.IdentityModel.Tokens.Tests
{
    /// <summary>
    /// The purpose of these tests are to ensure that Saml, Saml2 and Jwt handling
    /// results in the same exceptions, claims etc.
    /// </summary>
    public class CrossTokenTests
    {
        [Theory, MemberData(nameof(CrossTokenValidateTokenTheoryData))]
        public void CrossTokenValidateToken(CrossTokenTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.CrossTokenValidateToken", theoryData);
            try
            {
                var samlToken = IdentityUtilities.CreateEncodedSaml(theoryData.SecurityTokenDescriptor, theoryData.SamlTokenHandler);
                var saml2Token = IdentityUtilities.CreateEncodedSaml2(theoryData.SecurityTokenDescriptor, theoryData.Saml2TokenHandler);
                var jwtToken = IdentityUtilities.CreateEncodedJwt(theoryData.SecurityTokenDescriptor, theoryData.JwtTokenHandler);

                var samlPrincipal = theoryData.SamlTokenHandler.ValidateToken(samlToken, theoryData.TokenValidationParameters, out SecurityToken samlValidatedToken);
                var saml2Principal = theoryData.Saml2TokenHandler.ValidateToken(saml2Token, theoryData.TokenValidationParameters, out SecurityToken saml2ValidatedToken);
                var jwtPrincipal = theoryData.JwtTokenHandler.ValidateToken(jwtToken, theoryData.TokenValidationParameters, out SecurityToken jwtValidatedToken);

                // false = ignore type of objects, we expect all objects in the principal to be of same type (no derived types)
                context.IgnoreType = false;
                IdentityComparer.AreEqual(samlPrincipal, saml2Principal, context);

                // true = ignore properties of claims, any mapped claims short to long for JWT's will have a property that represents the short type.
                context.IgnoreProperties = true;
                IdentityComparer.AreEqual(samlPrincipal, jwtPrincipal, context);
                IdentityComparer.AreEqual(saml2Principal, jwtPrincipal, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<CrossTokenTheoryData> CrossTokenValidateTokenTheoryData
        {
            get
            {
                var jwtTokenHandler = new JwtSecurityTokenHandler();
                var samlTokenHandler = new SamlSecurityTokenHandler();
                var saml2TokenHandler = new Saml2SecurityTokenHandler();
                jwtTokenHandler.InboundClaimFilter.Add("aud");
                jwtTokenHandler.InboundClaimFilter.Add("exp");
                jwtTokenHandler.InboundClaimFilter.Add("iat");
                jwtTokenHandler.InboundClaimFilter.Add("iss");
                jwtTokenHandler.InboundClaimFilter.Add("nbf");

                return new TheoryData<CrossTokenTheoryData>
                {
                    new CrossTokenTheoryData
                    {
                        First = true,
                        JwtTokenHandler = jwtTokenHandler,
                        SamlTokenHandler = samlTokenHandler,
                        Saml2TokenHandler = saml2TokenHandler,
                        TestId = "AsymmetricSignToken",
                        SecurityTokenDescriptor = Default.SecurityTokenDescriptor(null, Default.AsymmetricSigningCredentials, Default.SamlClaimsIssuerEqOriginalIssuer),
                        TokenValidationParameters =  Default.AsymmetricSignTokenValidationParameters
                    }
                };
            }
        }

    }

    public class CrossTokenTheoryData : TheoryDataBase
    {
        public JwtSecurityTokenHandler JwtTokenHandler { get; set; }
        public SamlSecurityTokenHandler SamlTokenHandler { get; set; }
        public Saml2SecurityTokenHandler Saml2TokenHandler { get; set; }
        public SecurityTokenDescriptor SecurityTokenDescriptor { get; set; }
        public TokenValidationParameters TokenValidationParameters { get; set; }
    }
}

#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
