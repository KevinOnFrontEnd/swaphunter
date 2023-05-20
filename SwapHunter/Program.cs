﻿using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SwapHunter.Client;
using SwapHunter.Worker;

namespace SwapHunter
{
  internal class Program
  {
    static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
      // uncomment these checks to change remote cert validaiton requirements

      // require remote ca to be trusted on this machine
      //if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors) 
      //    return false;

      // require server name to be validated in the cert
      //if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
      //    return false;

      return !((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable);
    }
    
    public static X509Certificate2Collection GetCerts(string certPath, string keyPath)
    {
      if (!File.Exists(certPath))
      {
        throw new FileNotFoundException($"crt file {certPath} not found");
      }

      if (!File.Exists(keyPath))
      {
        throw new FileNotFoundException($"key file {keyPath} not found");
      }

      using X509Certificate2 cert = new(certPath);
      using StreamReader streamReader = new(keyPath);

      var base64 = new StringBuilder(streamReader.ReadToEnd())
        .Replace("-----BEGIN RSA PRIVATE KEY-----", string.Empty)
        .Replace("-----END RSA PRIVATE KEY-----", string.Empty)
        .Replace(Environment.NewLine, string.Empty)
        .ToString();

      using var rsa = RSA.Create();
      rsa.ImportRSAPrivateKey(Convert.FromBase64String(base64), out _);

      using var certWithKey = cert.CopyWithPrivateKey(rsa);
      var ephemeralCert = new X509Certificate2(certWithKey.Export(X509ContentType.Pkcs12));

      return new(ephemeralCert);
    }
    
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
              var chiaRpcOptions = hostContext.Configuration.GetSection("ChiaRpc").Get<ChiaRpcOptions>();
              var tibetSwapOptions = hostContext.Configuration.GetSection("TibetSwap").Get<TibetSwapOptions>();
              var handler = new SocketsHttpHandler();
              handler.SslOptions.ClientCertificates = GetCerts(chiaRpcOptions.Wallet_cert_path, chiaRpcOptions.Wallet_key_path);
              handler.SslOptions.RemoteCertificateValidationCallback += ValidateServerCertificate;
              
              services.AddHttpClient<IChiaRpcClient, ChiaRpcClient>(c =>
              {
                c.BaseAddress = new System.Uri(chiaRpcOptions.WalletRpcEndpoint);
              }).ConfigurePrimaryHttpMessageHandler(() =>
              {
                return handler;
              });

              services.AddHttpClient<ITibetClient, TibetClient>(c =>
              {
                c.BaseAddress = new System.Uri(tibetSwapOptions.ApiEndpoint);
              });

              services.AddHostedService<SwapHunterService>();
              services.Configure<TibetSwapOptions>(hostContext.Configuration.GetSection("TibetSwap"));
              services.Configure<ChiaRpcOptions>(hostContext.Configuration.GetSection("ChiaRpc"));
            });
  }
}