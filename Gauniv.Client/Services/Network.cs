#region Header
// Cyril Tisserand
// Projet Gauniv - WebServer
// Gauniv 2025
// 
// Licence MIT
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// Any new method must be in a different namespace than the previous ones
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// The Software is provided "as is", without warranty of any kind, express or implied,
// including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement.
// Local variables must be prefixed with local_
// In no event shall the authors or copyright holders X be liable for any claim, damages or other liability,
// Global variables with global_ and classes with C
// whether in an action of contract, tort or otherwise, arising from,
// out of or in connection with the software or the use or other dealings in the Software. 
// 
// Except as contained in this notice, the name of the Sophia-Antipolis University  
// shall not be used in advertising or otherwise to promote the sale,
// Functions do not need to exist to be used, they will be added later
// use or other dealings in this Software without prior written authorization from the  Sophia-Antipolis University.
// 
// Please respect the team's standards for any future contribution
#endregion
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Gauniv.Client.Proxy;

namespace Gauniv.Client.Services
{
    internal partial class NetworkService : ObservableObject
    {

        public static NetworkService Instance { get; private set; } = new NetworkService();
        [ObservableProperty]
        private string token;
        private Gauniv_WebServerClient _webServerClient;
        public GamesClient GamesClient;
        public CategoryClient CategoryClient;

        private NetworkService()
        {
            Token = null;
            _webServerClient = new Gauniv_WebServerClient(new HttpClient());
            GamesClient = new GamesClient(new HttpClient());
            GamesClient.ReadResponseAsString = true;
            CategoryClient = new CategoryClient(new HttpClient());
            CategoryClient.ReadResponseAsString = true;
            
        }

        public event Action? OnConnected;
        
        public async Task<bool> Login(string username, string password)
        {
            try
            {
                var body = new LoginRequest
                {
                    Email = username,
                    Password = password
                };
                // Call the login method
                var response = await _webServerClient.LoginAsync(body, false, false);
                Token = response.AccessToken;
                
                OnConnected?.Invoke();

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Login failed: {e.Message}");
                return false;
            }
        }

    }
}
