using iameewh.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace iameewh.Utility
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            try
            {
                if (db.Database.GetPendingMigrations().Any())
                {
                    await db.Database.MigrateAsync();
                }
            }
            catch { }

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                await roleManager.CreateAsync(new IdentityRole("Buyer"));
                await roleManager.CreateAsync(new IdentityRole("Seller"));
            }

            if (await userManager.FindByEmailAsync("admin@kimi.com") == null)
            {
                var user = new ApplicationUser
                {
                    UserName = "admin@kimi.com",
                    Email = "admin@kimi.com",
                    Name = "Đặng Hoàng Bảo Trâm",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "Admin123!");
                await userManager.AddToRoleAsync(user, "Admin");
            }

            if (!db.Categories.Any())
            {
                db.Categories.AddRange(new List<Category> {
                    new Category { Name = "Tiểu thuyết Cổ Chân Nhân" },
                    new Category { Name = "Siêu xe JDM - Supra" },
                    new Category { Name = "Điện thoại Samsung/Poco" }
                });
                await db.SaveChangesAsync();
            }
        }

        public static async Task SeedLocations(ApplicationDbContext db)
        {
            if (await db.Provinces.AnyAsync()) return;
            using var client = new HttpClient();
            try
            {
                var response = await client.GetFromJsonAsync<List<ProvinceJson>>("https://provinces.open-api.vn/api/?depth=3");
                if (response != null)
                {
                    foreach (var p in response)
                    {
                        var province = new Province { Id = p.code.ToString(), Name = p.name };
                        db.Provinces.Add(province);
                        foreach (var d in p.districts)
                        {
                            var district = new District { Id = d.code.ToString(), Name = d.name, ProvinceId = province.Id };
                            db.Districts.Add(district);
                            foreach (var w in d.wards)
                            {
                                db.Wards.Add(new Ward { Id = w.code.ToString(), Name = w.name, DistrictId = district.Id });
                            }
                        }
                    }
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        public class ProvinceJson { public int code { get; set; } public string name { get; set; } public List<DistrictJson> districts { get; set; } }
        public class DistrictJson { public int code { get; set; } public string name { get; set; } public List<WardJson> wards { get; set; } }
        public class WardJson { public int code { get; set; } public string name { get; set; } }
    }
}