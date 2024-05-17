using AutoMapper;
using Core.Hal.Example.Model.Users.ViewModels;
using Core.Hal.Example.Model.Users;
using Microsoft.AspNetCore.Mvc;
using AutoFixture;
using Core.Hal.Example.Model.Users.Commands;
using Core.Hal.Example.Model;

namespace Core.Hal.Example.Controllers
{
    [ServiceFilter(typeof(SupportsHalAttribute))]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMapper _mapper;
        public UserController(IMapper mapper)

        {
            _mapper = mapper;
        }

        [HttpGet(Name = "GetUsers")]
        public IPagedList<UserSummary> Get()
        {
            Database db = new Database(_mapper);


            CreateTestDataIn(db);

            var roles = db.GetAllUsersPaged(new Model.Users.Queries.GetUserList());

            return roles;
        }

        private static void CreateTestDataIn(Database db)
        {
            var createRole = new CreateRole
            {
                Name = "Admin",
                Permissions = new[] { "View Users", "Edit Users", "Deactivate Users" }
            };
            db.CreateRole(createRole);
            db.CreateUser(new CreateUser { FullName = "Dan Barua", UserName = "dan", RoleId = createRole.Id.GetValueOrDefault() });
            db.CreateUser(new CreateUser { FullName = "Jonathon Channon", UserName = "jonathon", RoleId = createRole.Id.GetValueOrDefault() });

            // let's generate some random data!
            var fixture = new Fixture { RepeatCount = 100 };

            var roles = fixture.CreateMany<CreateRole>().ToList();
            var users = fixture.CreateMany<CreateUser>().ToList();
            foreach (var r in roles)
            {
                db.CreateRole(r);
            }

            var roleCount = roles.Count();
            foreach (var u in users)
            {
                u.RoleId = roles.Skip(new Random().Next(0, roleCount)).Take(1).First().Id.GetValueOrDefault();
                // select random id from roles
                db.CreateUser(u);
            }
        }
    }
}
