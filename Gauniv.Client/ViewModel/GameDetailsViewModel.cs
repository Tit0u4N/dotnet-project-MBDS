using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gauniv.Client.Proxy;

namespace Gauniv.Client.ViewModel
{
    
    [QueryProperty(nameof(GameRef), "game")]
    public partial class GameDetailsViewModel : ObservableObject
    {
        [ObservableProperty]
        private GameFullDto game;

        public GameFullDto GameRef
        {
            get => game;
            set
            {
                if (SetProperty(ref game, value))
                {
                    BuildArgsDisplay();
                }
            }
        }


        [ObservableProperty]
        private string argsDisplay = string.Empty;
        
        
        private void BuildArgsDisplay()
        {
            if (game == null || game.Name == null)
            {
                ArgsDisplay = "No arguments available.";
                return;
            }
            
            // affiche les données de game dans argsDisplay
            var argsList = new List<string>
            {
                $"ID: {game.Id}",
                $"Name: {game.Name}",
                $"Description: {game.Description}",
                $"Price: {game.Price:C}",
                $"Release Date: {game.ReleaseDate:yyyy-MM-dd}",
                $"Rating: {game.Rating}/5",
            };
            ArgsDisplay = string.Join(Environment.NewLine, argsList);
        }
    }
}
