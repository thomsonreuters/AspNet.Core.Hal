What is Hal?
===========
[Specification](http://stateless.co/hal_specification.html)


Get started
=============
1) Install the AspnetCore.Hal.SystemTextHalJsonFormatter package if we are using System.Text.Json 
```powershell
Install-Package AspnetCore.Hal.NewtonsoftHalJsonFormatter
```
   Install the AspnetCore.Hal.NewtonsoftHalJsonFormatter package if we are using Newtonsoft  

```powershell
Install-Package AspnetCore.Hal.NewtonsoftHalJsonFormatter
```






2) Create a `HalConfiguration` instance.
```csharp
var config = new HalConfiguration();

//simple example - creates a "self" link templated with the user's id
config.For<UserSummary>()
    .Links(model => new Link("self", "/users/{id}").CreateLink(model));

//complex example - creates paging links populated with query string search terms
config.For<PagedList<UserSummary>>()
      .Embeds("users", x => x.Data)
      .Links(
          (model, ctx) =>
          LinkTemplates.Users.GetUsersPaged.CreateLink("self", ctx.Request.Query, new { blah = "123" }))
      .Links(
          (model, ctx) =>
          LinkTemplates.Users.GetUsersPaged.CreateLink("next", ctx.Request.Query, new { page = model.PageNumber + 1 }),
          model => model.PageNumber < model.TotalPages)
      .Links(
          (model, ctx) =>
          LinkTemplates.Users.GetUsersPaged.CreateLink("prev", ctx.Request.Query, new { page = model.PageNumber - 1 }),
          model => model.PageNumber > 0);


//per request configuration
 [ApiController]
    [Route("[controller]")]
    public class RolesController : Controller
    {
        private  readonly IMapper _mapper;    
        public RolesController(IMapper mapper) 
        
        {
            _mapper = mapper;
        }

        [HttpGet(Name = "GetRoles")]
        public IEnumerable<Role> Get()
        {
            Database db=new Database(_mapper);   
            CreateTestDataIn(db);
            var roles = db.GetAllRoles();
            return roles;
        }
}
```

3) Register it in Program.cs of your application as below.
```csharp
builder.Services.AddSingleton<IProvideHalTypeConfiguration>(provider => Halconfig.HypermediaConfiguration());

builder.Services.AddHalSupport();
```

4) Set your `Accept` header to `application/hal+json`

