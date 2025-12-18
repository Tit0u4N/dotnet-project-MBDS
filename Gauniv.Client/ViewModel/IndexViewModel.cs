using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Pages;
using Gauniv.Client.Services;
using Gauniv.Client.Proxy;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    public partial class IndexViewModel: ObservableObject
    {
        // Collection affichée
        public ObservableCollection<GameFullDto> Games { get; } = new ObservableCollection<GameFullDto>();

        // Options de pagination
        public int[] PageSizes => new[] { 12, 24, 48 };

        [ObservableProperty]
        private int pageSize = 12;

        [ObservableProperty]
        private int pageIndex = 0;

        [ObservableProperty]
        private int totalItems;

        [ObservableProperty]
        private int totalPages;

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        //Selected item bindé depuis le XAML
        [ObservableProperty]
        private GameFullDto? selectedGame;

        public int CurrentPageDisplay => Math.Max(1, PageIndex + 1);

        public IndexViewModel()
        {
            // Lancer le chargement initial sans bloquer le constructeur
            _ = LoadGamesAsync();
        }

        partial void OnPageSizeChanged(int value)
        {
            // Reset page and reload when page size changes
            PageIndex = 0;
            _ = LoadGamesAsync();
        }

        partial void OnPageIndexChanged(int value)
        {
            // Mettre à jour l'affichage de la page
            OnPropertyChanged(nameof(CurrentPageDisplay));
            _ = LoadGamesAsync();
        }

        partial void OnTotalPagesChanged(int value)
        {
            OnPropertyChanged(nameof(CurrentPageDisplay));
        }

        // Appelé automatiquement lorsque SelectedGame change via le binding XAML
        partial void OnSelectedGameChanged(GameFullDto? value)
        {
            // value peut être null quand la sélection est désélectionnée; ignorer
            if (value == null) return;

            // Navigation asynchrone fire-and-forget (UI thread)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Naviguer vers la page de détails
                    var args = new Dictionary<string, object> { { "game", value } };
                    NavigationService.Instance.Navigate<GameDetails>(args);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                finally
                {
                    // reset selection pour permettre de recliquer le même élément
                    SelectedGame = null;
                }
            });
        }

        [RelayCommand]
        private async Task LoadGamesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var offset = PageIndex * PageSize;
                // si SearchQuery vide, envoyer null pour ne pas filtrer
                var name = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;

                var dto = await NetworkService.Instance.GamesClient.AllAsync(offset: offset, limit: PageSize, name: name);

                TotalItems = dto.Total;
                TotalPages = dto.TotalPages;

                Games.Clear();
                if (dto.Results != null)
                await MainThread.InvokeOnMainThreadAsync(() =>{
                    Games.Clear();
                    foreach (var g in dto.Results)
                        Games.Add(g);
                    
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private Task NextPageAsync()
        {
            if (TotalPages <= 0) return Task.CompletedTask;
            if (PageIndex < TotalPages - 1)
            {
                PageIndex++;
                // LoadGamesAsync sera appelé automatiquement depuis OnPageIndexChanged
            }
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task PrevPageAsync()
        {
            if (PageIndex > 0)
            {
                PageIndex--;
            }
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task ChangePageSizeAsync(int newSize)
        {
            if (newSize <= 0) return Task.CompletedTask;
            PageSize = newSize;
            PageIndex = 0;
            // LoadGamesAsync sera déclenché par OnPageSizeChanged
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            PageIndex = 0;
            await LoadGamesAsync();
        }
    }
}
