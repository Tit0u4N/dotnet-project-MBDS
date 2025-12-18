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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.HighPerformance;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Models;
using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using X.PagedList.Extensions;

namespace Gauniv.WebServer.Controllers
{
    public class HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext applicationDbContext,
        UserManager<User> userManager,
        GameService gameService
        ) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly ApplicationDbContext applicationDbContext = applicationDbContext;
        private readonly UserManager<User> userManager = userManager;
        private readonly GameService gameService = gameService;

        public IActionResult Index()
        {
            return RedirectToAction("Shop");
        }

        public async Task<IActionResult> Shop(
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 20,
            [FromQuery] string? name = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null)
        {
             var (games, total) = await gameService.GetAllGamesAsync(name, minPrice, maxPrice, null, null, null, offset, limit);
             
             ViewBag.Total = total;
             ViewBag.Offset = offset;
             ViewBag.Limit = limit;
             ViewBag.Name = name;
             ViewBag.MinPrice = minPrice;
             ViewBag.MaxPrice = maxPrice;

             return View(games);
        }

        [Authorize]
        public async Task<IActionResult> MyGames(
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 20,
            [FromQuery] string? name = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index", "Home");

            var (games, total) = await gameService.GetAllGamesAsync(name, minPrice, maxPrice, null, true, user.Id, offset, limit);

            ViewBag.Total = total;
            ViewBag.Offset = offset;
            ViewBag.Limit = limit;
            ViewBag.Name = name;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(games);
        }

        public async Task<IActionResult> Details(int id)
        {
            var game = await gameService.GetGameByIdAsync(id);
            if (game == null) return NotFound();

            var user = await userManager.GetUserAsync(User);
            if (user != null)
            {
                var userGame = await applicationDbContext.Set<UserGame>()
                    .FirstOrDefaultAsync(ug => ug.User.Id == user.Id && ug.Game.Id == id);
                
                if (userGame != null)
                {
                    ViewBag.IsOwned = true;
                    ViewBag.PurchaseDate = userGame.PurchaseDate;
                }
                else
                {
                     ViewBag.IsOwned = false;
                }
            }
            else
            {
                 ViewBag.IsOwned = false;
            }

            return View(game);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Buy(int id)
        {
             var user = await userManager.GetUserAsync(User);
             if (user == null) return Challenge();

             var success = await gameService.BuyGameAsync(id, user.Id);
             if (success)
             {
                 return RedirectToAction("PurchaseSuccess", new { id = id });
             }
             
             // Identify why it failed? (Already owned, etc.) for now just redirect to details with error?
             // Or redirect to MyGames logic.
             return RedirectToAction("Details", new { id = id }); 
        }

        [Authorize]
        public async Task<IActionResult> PurchaseSuccess(int id)
        {
            var game = await gameService.GetGameByIdAsync(id);
            if (game == null) return NotFound();
            return View(game);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}