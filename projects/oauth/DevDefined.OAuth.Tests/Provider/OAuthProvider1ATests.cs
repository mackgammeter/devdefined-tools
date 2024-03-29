using System;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Provider;
using DevDefined.OAuth.Provider.Inspectors;
using DevDefined.OAuth.Testing;
using NUnit.Framework;

namespace DevDefined.OAuth.Tests.Provider
{
  [TestFixture]
  public class OAuthProvider1ATests
  {
    OAuthProvider provider;

    [TestFixtureSetUp]
    public void SetUpProvider()
    {
      var tokenStore = new TestTokenStore();
      var consumerStore = new TestConsumerStore();
      var nonceStore = new TestNonceStore();

      provider = new OAuthProvider(tokenStore,
                                   new SignatureValidationInspector(consumerStore),
                                   new NonceStoreInspector(nonceStore),
                                   new TimestampRangeInspector(new TimeSpan(1, 0, 0)),
                                   new ConsumerValidationInspector(consumerStore),
                                   new OAuth10AInspector(tokenStore));
    }

    IOAuthSession CreateConsumer(string signatureMethod)
    {
      var consumerContext = new OAuthConsumerContext
        {
          SignatureMethod = signatureMethod,
          ConsumerKey = "key",
          ConsumerSecret = "secret",
          Key = TestCertificates.OAuthTestCertificate().PrivateKey
        };

      var session = new OAuthSession(consumerContext, "http://localhost/oauth/requesttoken.rails",
                                     "http://localhost/oauth/userauhtorize.rails",
                                     "http://localhost/oauth/accesstoken.rails");

      return session;
    }

    [Test]
    public void ExchangeTokensWhenVerifierIsMatchDoesNotThrowException()
    {
      IOAuthSession session = CreateConsumer(SignatureMethod.RsaSha1);
      IOAuthContext context = session.BuildExchangeRequestTokenForAccessTokenContext(
        new TokenBase {ConsumerKey = "key", Token = "requestkey"}, "GET", "GzvVb5WjWfHKa/0JuFupaMyn").Context;
      provider.ExchangeRequestTokenForAccessToken(context);
    }

    [Test]
    [ExpectedException(typeof (OAuthException), ExpectedMessage = "Missing required parameter : oauth_verifier")]
    public void ExchangeTokensWhenVerifierIsMissingThrowsException()
    {
      string verifier = null;

      IOAuthSession session = CreateConsumer(SignatureMethod.RsaSha1);
      IOAuthContext context = session.BuildExchangeRequestTokenForAccessTokenContext(
        new TokenBase {ConsumerKey = "key", Token = "requestkey"}, "GET", verifier).Context;
      provider.ExchangeRequestTokenForAccessToken(context);
    }

    [Test]
    [ExpectedException(typeof(OAuthException), ExpectedMessage = "The parameter \"oauth_verifier\" was rejected")]
    public void ExchangeTokensWhenVerifierIsWrongThrowsException()
    {
      IOAuthSession session = CreateConsumer(SignatureMethod.RsaSha1);
      IOAuthContext context = session.BuildExchangeRequestTokenForAccessTokenContext(
        new TokenBase {ConsumerKey = "key", Token = "requestkey"}, "GET", "wrong").Context;
      provider.ExchangeRequestTokenForAccessToken(context);
    }

    [Test]
    public void RequestTokenWithCallbackDoesNotThrowException()
    {
      IOAuthSession session = CreateConsumer(SignatureMethod.PlainText);
      IOAuthContext context = session.BuildRequestTokenContext("GET").Context;
      provider.GrantRequestToken(context);
    }

    [Test]
    [ExpectedException(typeof(OAuthException), ExpectedMessage = "Missing required parameter : oauth_callback")]
    public void RequestTokenWithoutCallbackWillThrowException()
    {
      IOAuthSession session = CreateConsumer(SignatureMethod.PlainText);
      IOAuthContext context = session.BuildRequestTokenContext("GET").Context;
      context.CallbackUrl = null; // clear parameter, as it will default to "oob"
      provider.GrantRequestToken(context);
    }
  }
}