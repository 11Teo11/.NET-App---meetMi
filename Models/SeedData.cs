using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProiectDotNet.Data;

namespace ProiectDotNet.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider
        serviceProvider)
        {
            using (var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService
            <DbContextOptions<ApplicationDbContext>>()))
            {
                // verificam daca in baza de date exista cel putin un rol
                // insemnand ca a fost rulat codul
                // de aceea facem return pentru a nu insera rolurile inca o data
                // acesta metoda trebuie sa se execute o singura data
                if (context.Roles.Any())
                {
                    return; // baza de date contine deja roluri
                }

                // crearea rolurilor in bd
                // daca nu contine roluri, acestea se vor crea
                context.Roles.AddRange(

                new IdentityRole
                {
                    Id = "040fdb67-9b39-47f9-9bd4-90c52b38e741",
                    Name = "Admin",
                    NormalizedName = "Admin".ToUpper()
                },

                new IdentityRole
                {
                    Id = "234f9f59-b825-49f1-b69e-0849c59bada5",
                    Name = "User",
                    NormalizedName = "User".ToUpper()
                }

            );

                // o noua instanta pe care o vom utiliza pentru crearea parolelor utilizatorilor
                // parolele sunt de tip hash
                var hasher = new PasswordHasher<ApplicationUser>();

                // crearea userilor in bd
                // se creeaza cate un user pentru fiecare rol
                context.Users.AddRange(
                new ApplicationUser
                {

                    Id = "96d194ac-b82c-447d-90b3-84460fbb25f6",
                    // primary key
                    UserName = "admin@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "ADMIN@TEST.COM",
                    Email = "admin@test.com",
                    NormalizedUserName = "ADMIN@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Admin1!"),

                    LastName = "Admin",
                    FirstName = "Admin",
                    Description = "Contul de administrator al platformei.",
                    Visibility = "public",
                    ProfilePicture = "/images/seed/profile_admin.jpg"
                },


                new ApplicationUser
                {

                    Id = "0aa86054-85bd-41d6-9a98-1e4b53efc838",
                    // primary key
                    UserName = "user@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER@TEST.COM",
                    Email = "user@test.com",
                    NormalizedUserName = "USER@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "User1!"),

                    LastName = "Bobitzu",
                    FirstName = "Teutzu",
                    Description = "Un utilizator de bază al platformei.",
                    Visibility = "private",
                    ProfilePicture = "/images/seed/profile_teutzu.jpg"
                },

                // adaugam utilizatori noi pentru diversitate
                new ApplicationUser
                {
                    Id = "d2b59174-89c1-4b14-878e-56e632ef47c1",
                    UserName = "user3@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER3@TEST.COM",
                    Email = "user3@test.com",
                    NormalizedUserName = "USER3@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "User3!"),
                    LastName = "Ionescu",
                    FirstName = "Maria",
                    Description = "Pasionata de fotografie si calatorii.",
                    Visibility = "public",
                    ProfilePicture = "/images/seed/profile_maria.jpg"
                },

                new ApplicationUser
                {
                    Id = "e3c60285-90d2-5c25-989f-67f743f058d2",
                    UserName = "user4@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER4@TEST.COM",
                    Email = "user4@test.com",
                    NormalizedUserName = "USER4@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "User4!"),
                    LastName = "Popa",
                    FirstName = "Andrei",
                    Description = "Software developer si gamer pasionat.",
                    Visibility = "public",
                    ProfilePicture = "/images/seed/profile_andrei.jpg"
                },

                new ApplicationUser
                {
                    Id = "f4d71396-01e3-6d36-090a-78f8540169e3",
                    UserName = "user5@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER5@TEST.COM",
                    Email = "user5@test.com",
                    NormalizedUserName = "USER5@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "User5!"),
                    LastName = "Radu",
                    FirstName = "Elena",
                    Description = "Life coach si pasionata de gatit sanatos.",
                    Visibility = "private",
                    ProfilePicture = "/images/seed/profile_elena.jpg"
                }
                );

                // asocierea user-role
                context.UserRoles.AddRange(
                new IdentityUserRole<string>
                {

                    RoleId = "040fdb67-9b39-47f9-9bd4-90c52b38e741",

                    UserId = "96d194ac-b82c-447d-90b3-84460fbb25f6"
                },

                new IdentityUserRole<string>

                {

                    RoleId = "234f9f59-b825-49f1-b69e-0849c59bada5",

                    UserId = "0aa86054-85bd-41d6-9a98-1e4b53efc838"
                },

                // asociem rolurile si pentru userii noi
                new IdentityUserRole<string>
                {
                    RoleId = "234f9f59-b825-49f1-b69e-0849c59bada5",
                    UserId = "d2b59174-89c1-4b14-878e-56e632ef47c1"
                },
                new IdentityUserRole<string>
                {
                    RoleId = "234f9f59-b825-49f1-b69e-0849c59bada5",
                    UserId = "e3c60285-90d2-5c25-989f-67f743f058d2"
                },
                new IdentityUserRole<string>
                {
                    RoleId = "234f9f59-b825-49f1-b69e-0849c59bada5",
                    UserId = "f4d71396-01e3-6d36-090a-78f8540169e3"
                }
                );

                // crearea grupurilor noi pentru comunitate
                var groupTech = new Group { Name = "Tech Geeks", Description = "Hardware and coding discussion.", IsPublic = true, ModeratorId = "e3c60285-90d2-5c25-989f-67f743f058d2", CoverPhoto = "/images/seed/group_tech.jpg" };
                var groupTravel = new Group { Name = "Travel Buddies", Description = "Exploring the world together.", IsPublic = true, ModeratorId = "d2b59174-89c1-4b14-878e-56e632ef47c1", CoverPhoto = "/images/seed/group_travel.jpg" };
                var groupHealth = new Group { Name = "Healthy Living", Description = "Fitness and nutrition tips.", IsPublic = false, ModeratorId = "f4d71396-01e3-6d36-090a-78f8540169e3", CoverPhoto = "/images/seed/group_healthy.jpg" };

                context.Groups.AddRange(groupTech, groupTravel, groupHealth);
                context.SaveChanges();

                // adaugam membrii in grupuri
                context.GroupMembers.AddRange(
                    new GroupMember { GroupId = groupTech.Id, UserId = "e3c60285-90d2-5c25-989f-67f743f058d2", IsAccepted = true },
                    new GroupMember { GroupId = groupTech.Id, UserId = "d2b59174-89c1-4b14-878e-56e632ef47c1", IsAccepted = true },
                    new GroupMember { GroupId = groupTravel.Id, UserId = "d2b59174-89c1-4b14-878e-56e632ef47c1", IsAccepted = true },
                    new GroupMember { GroupId = groupHealth.Id, UserId = "f4d71396-01e3-6d36-090a-78f8540169e3", IsAccepted = true },
                    new GroupMember { GroupId = groupHealth.Id, UserId = "0aa86054-85bd-41d6-9a98-1e4b53efc838", IsAccepted = true }
                );

                // adaugam mesaje in cadrul grupurilor
                context.Messages.AddRange(
                    new Message { GroupId = groupTech.Id, UserId = "e3c60285-90d2-5c25-989f-67f743f058d2", Content = "Just got a new GPU! It is amazing.", Date = DateTime.Now.AddDays(-1) },
                    new Message { GroupId = groupHealth.Id, UserId = "f4d71396-01e3-6d36-090a-78f8540169e3", Content = "Look at this healthy lunch.", Date = DateTime.Now }
                );

                // adaugam manual cele zece postari globale cu descrieri realiste
                context.Posts.AddRange(
                    new Post { UserId = "96d194ac-b82c-447d-90b3-84460fbb25f6", Media = "/images/seed/post_car.jpg", Text = "Nothing beats the feeling of the open road and a powerful engine.", Date = DateTime.Now.AddHours(-1) },
                    new Post { UserId = "0aa86054-85bd-41d6-9a98-1e4b53efc838", Media = "/images/seed/post_cat.jpg", Text = "Found this little guy taking a nap in the sun. Pure bliss.", Date = DateTime.Now.AddHours(-3) },
                    new Post { UserId = "d2b59174-89c1-4b14-878e-56e632ef47c1", Media = "/images/seed/post_city.jpg", Text = "The city lights at night have a magic of their own. Exploring the urban jungle.", Date = DateTime.Now.AddHours(-5) },
                    new Post { UserId = "e3c60285-90d2-5c25-989f-67f743f058d2", Media = "/images/seed/post_coding.jpg", Text = "Deep into a coding session. Finally fixed that stubborn bug!", Date = DateTime.Now.AddHours(-8) },
                    new Post { UserId = "f4d71396-01e3-6d36-090a-78f8540169e3", Media = "/images/seed/post_coffee.jpg", Text = "Starting the morning with a perfect latte and some quiet time.", Date = DateTime.Now.AddHours(-12) },
                    new Post { UserId = "96d194ac-b82c-447d-90b3-84460fbb25f6", Media = "/images/seed/post_concert.jpg", Text = "The energy tonight was incredible! Live music is back.", Date = DateTime.Now.AddHours(-15) },
                    new Post { UserId = "0aa86054-85bd-41d6-9a98-1e4b53efc838", Media = "/images/seed/post_food.jpg", Text = "Tried a new recipe today. Best pasta I have made so far!", Date = DateTime.Now.AddHours(-18) },
                    new Post { UserId = "d2b59174-89c1-4b14-878e-56e632ef47c1", Media = "/images/seed/post_gaming.jpg", Text = "Finally finished my new RGB setup. Time to test the high scores.", Date = DateTime.Now.AddHours(-21) },
                    new Post { UserId = "e3c60285-90d2-5c25-989f-67f743f058d2", Media = "/images/seed/post_mountain.jpg", Text = "Endless views and fresh mountain air. Nature is the best therapy.", Date = DateTime.Now.AddDays(-1) },
                    new Post { UserId = "f4d71396-01e3-6d36-090a-78f8540169e3", Media = "/images/seed/post_reading.jpg", Text = "A rainy afternoon and a good book. The perfect cozy combination.", Date = DateTime.Now.AddDays(-1).AddHours(-4) }
                );

                context.SaveChanges();
            }
        }
    }
}