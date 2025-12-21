using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Pages;
using Gauniv.Client.Proxy;
using Gauniv.Client.Services;

namespace Gauniv.Client.ViewModel
{
    public partial class MyGamesViewModel: ObservableObject
    {
        // Collection affichée
        public ObservableCollection<GameFullDto> Games
        {
            get;
            private set => SetProperty(ref field, value);
        } = new ObservableCollection<GameFullDto>([]);

        public int[] PageSizes => new[] { 12, 24, 48 };
        
        
        [ObservableProperty]
        private int gridSpan = 6;
        
        [ObservableProperty]
        private int pageSize = 12;

        [ObservableProperty]
        private int pageIndex;

        [ObservableProperty]
        private int totalItems;

        [ObservableProperty]
        private int totalPages;

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isNotUpdating;
        
        [ObservableProperty]
        private bool isPrevPageEnabled = true;
        
        [ObservableProperty]
        private bool isNextPageEnabled = true;

        [ObservableProperty]
        private string? errorMessage;

        //Remplacé : on stocke la saisie utilisateur comme string pour pouvoir la nettoyer/formatter
        [ObservableProperty]
        private string minPriceString = string.Empty;
        
        [ObservableProperty]
        private string maxPriceString = string.Empty;
        
        [ObservableProperty]
        private CategoryDto[] categories = Array.Empty<CategoryDto>();

        //Selected item bindé depuis le XAML
        [ObservableProperty]
        private GameFullDto? selectedGame;

        // Flags pour éviter les boucles lors du nettoyage/assignation
        private bool _suppressMinPriceSanitize;
        private bool _suppressMaxPriceSanitize;

        public int CurrentPageDisplay => Math.Max(1, PageIndex + 1);

        public MyGamesViewModel()
        {
            // Lancer le chargement initial sans bloquer le constructeur
            _ = Task.WhenAll(LoadCategoriesAsync(), LoadGamesAsync());
            isNotUpdating = true;
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                ICollection<CategoryFullDto> cats = await NetworkService.Instance.CategoryClient.AllAsync();
                Categories = cats.Select(c => new CategoryDto { Id = c.Id, Title = c.Title, IsSelected = false }).ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load categories: " + ex.Message);
            }
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

        partial void OnSelectedGameChanged(GameFullDto? value)
        {
            if (value == null) return;

            // Navigation asynchrone fire-and-forget (UI thread)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Naviguer vers la page de détails
                    var args = new Dictionary<string, object> { { "gameId", value.Id.ToString()} };
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
        
        partial void OnMinPriceStringChanged(string value)
        {
            if (_suppressMinPriceSanitize) return;
            _suppressMinPriceSanitize = true;

            MinPriceString = CleanPriceString(value);
            OnPropertyChanged(nameof(MinPriceString));

            _suppressMinPriceSanitize = false;
        }

        // Même logique pour MaxPrice
        partial void OnMaxPriceStringChanged(string value)
        {
            if (_suppressMaxPriceSanitize) return;
            _suppressMaxPriceSanitize = true;

            MaxPriceString = CleanPriceString(value);
            OnPropertyChanged(nameof(MaxPriceString));

            _suppressMaxPriceSanitize = false;
        }

        private string CleanPriceString(string? s, bool forceToCorrect = false)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var cleaned = new string((s ?? string.Empty).Where(c =>  c == '.' || c == ',' || char.IsDigit(c) ).ToArray());
            if (string.IsNullOrWhiteSpace(cleaned)) return string.Empty;

            // standardiser sur '.' pour parsing invariant
            cleaned = cleaned.Replace(',', '.');

            // Gérer plusieurs points : garder seulement le premier comme séparateur décimal
            int firstDot = cleaned.IndexOf('.');
            if (firstDot >= 0)
            {
                var secondPart = cleaned.Substring(firstDot + 1).Replace(".", string.Empty);
                cleaned = cleaned.Substring(0, firstDot + 1);
                if(secondPart != string.Empty)
                {
                    // garder seulement les deux premiers chiffres après la virgule
                    if(secondPart.Length > 2) 
                        secondPart = secondPart.Substring(0, 2);
                    // compléter avec des zéros si nécessaire pour avoir deux chiffres après la virgule
                    if(forceToCorrect)
                        secondPart = secondPart.PadRight(2, '0');
                    cleaned += secondPart;
                }
            }
            else if (forceToCorrect)
            {
                cleaned += ".00";
            }
            return cleaned;
        }

        private double? ParsePrice(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var cleaned = CleanPriceString(s, true);

            if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                return v;
            return null;
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

                // Préparer les filtres de prix à partir des chaînes nettoyées
                var minPrice = ParsePrice(MinPriceString);
                var maxPrice = ParsePrice(MaxPriceString);
                
                // Récupérer les catégories sélectionnées depuis la liste client-side
                var selectedCategoryIds = Categories?.Where(c => c.IsSelected).Select(c => c.Id) ?? Enumerable.Empty<int>();
                
                var dto = await NetworkService.Instance.GamesClient.AllAsync(offset: offset, limit: PageSize, name: name, minPrice: minPrice, maxPrice: maxPrice, category: selectedCategoryIds, owned:true);

                TotalItems = dto.Total;
                TotalPages = dto.TotalPages;
                
                IsPrevPageEnabled = PageIndex > 0;
                IsNextPageEnabled = PageIndex < TotalPages - 1;
                
                var newGames = dto.Results.Count > 0 ? new ObservableCollection<GameFullDto>(dto.Results.ToList()) : new ObservableCollection<GameFullDto>();
                
                await MainThread.InvokeOnMainThreadAsync(() =>{
                    try
                    {
                        IsNotUpdating  = false;
                        SelectedGame = null;
                        var span = Math.Min(6, Math.Max(1, newGames.Count));
                        GridSpan = span;
                        Games = newGames;
                        IsNotUpdating  = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
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
