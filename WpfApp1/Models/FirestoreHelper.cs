using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace WpfApp1.Models
{
    internal class FirestoreHelper
    {
        static string fireconfig = @"{
        ""type"": ""service_account"",
        ""project_id"": ""fir-5b855"",
        ""private_key_id"": ""c38bc10ce3b7166cded37825b8faa8c335b479de"",
        ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCJ1cfAd0NvQI6W\nRP0fIypp58wcgLlrJcb2DHKE9Y+/Rb28VCvAiGy/eQf7eZzNZx3Cnq0f9+wrCS3y\nMY32Fo//sok7wpoWYN54H3dZtkxpVc3wG2SbSW6vPHIT3qtSLfT5De7lJT3Me0Xq\nYpoTm13oztbbRLR0idyrkZYRVZfGi9zIdfoBr4Z1D+HSeROt1agD+kwtUd34D3X6\nf41gY8rPQh2MPRQ+FyLkzWcLC0DnIYh97BeHcjbhRtyiMJgUv5EeO8eGTTEoQtyM\nFgOMvTSnb+w7owSNcFfoP0p+J3RMKUrbyzeDjXIjtfrptu7/GfoaJYFpy9ojmN0V\nkyfnJwtvAgMBAAECggEAJfOcDY2JHs/astuKCpHHMuPlGpAC/dKoBsWEnsFydAsC\n5CEU7u1hbBMqNH7WwuO6mQTRzHSaLXtYkFA+s1yhB5mkGbVKchD4EIExfp0oSvSa\nQJt6RxugA1YUXw43g+gthInmlmd7rZrftFqz6+QipmCVXkh7m37+KKtsc5dqs/sV\nO02MoEarxu/DvGQAdfTXF3rEj8wMZBwvUrAuYRiVKHiyjAe7dx2r90LD3K2xYsk3\nXlJxhCl13MCshhpIj2rhyiornz4Wcs0VS9mEI6iegkhMd5sDchMLvn9xgpQfS6SK\nWAX9yA3EDashrRuizQFtVHgN0Wl8kzT4qa0t9TXzEQKBgQC91ruJJtKQnp/IHeeG\ncYclRS6e8+M/CbkRNYGTjk1nLtXYFBU6FPDbHjRfExxPBCB+1i9sNAum6o6Uyese\nOTB+xGJTLMLQSV0MFgXfPbEZ64lSAs05GQPf619EZfVmiGSF6bHILk3+6tMOVSBu\ne229WQ/UUnypZfMbOXrTct27OQKBgQC531El0tuKdrKu+ecoHOva99tS9c8zmx+9\nd4Z3NqTKzmA5/wLRVrcnhBu3wBJ+95veqWZGMao0V7ilRIwlH9j+9P8RDQHSMjHe\nPMs1YxxcLct3euHA4mgaDLDPZ32SV+F0g1MM0cy/p394WE3PdFyv+wSXWa9R7ZAG\nZqUnHzzz5wKBgHgXCNeAOZ/G2KkNUdMYqjeHhjCDc/QCwJIEWQ3w7UIivKBORAdU\nC/FxMAwc2MGbiLrz31gBrIVQyBWTjiq2XtkyfkjDfhGo9zWYEOrh6dDN0TaSEyTV\nkD/sc32fShgsm/qilRZfRHPINO9SJov9hLRNTNgxvi6jEaDdQbfVaDLxAoGAYl5p\nl9bsEW+YSTpAt932hMA/9rvYmLs0JRWouFbXB+4IxyjK2PdHn0YvVSP1pfRtLX4B\nfoyyQ1lZgz2v3cXpFaWbh+6WVCP0eGU8NljpnW8vC2ChMW+hIIgu2tUug9C2pO8L\nePFfpt6Ce7JgG7a9hvUWDPON8ZIEcx7HsNi7bWkCgYAtHTUjaJIOANr/D6ZBhzpq\np+J8Er9fjOaeqPAchMaLMVZ2shhXK2pfPZfLYIxNzoyRhgZSeMWREz5hsxQlYJhS\nIWO6mzkwMJ5A+k0TeOFNdNsTAbRUbjV/mYd7wYG6bsn+WgxHesb4/qIu55+M6oM1\n35rAfdX6WnBvJcJ4TyTaNg==\n-----END PRIVATE KEY-----\n"",
        ""client_email"": ""firebase-adminsdk-fbsvc@fir-5b855.iam.gserviceaccount.com"",
        ""client_id"": ""117419007231275047144"",
        ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
        ""token_uri"": ""https://oauth2.googleapis.com/token"",
        ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
        ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-fbsvc%40fir-5b855.iam.gserviceaccount.com"",
        ""universe_domain"": ""googleapis.com""
        }";
        static string filePath = "";
        public static FirestoreDb database { get; private set; }
        static FirestoreHelper()
        {
            string projectId = "fir-5b855";
            database = FirestoreDb.Create(projectId);
        }
        public static void setEnviromentVariable()
        {
            filePath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName())) + ".json";
            File.WriteAllText(filePath, fireconfig);
            File.SetAttributes(filePath, FileAttributes.Hidden);
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filePath);
            database = FirestoreDb.Create("fir-5b855");
            File.Delete(filePath);
        }
    }
}
