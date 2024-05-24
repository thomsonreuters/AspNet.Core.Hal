using AutoMapper;
using AspnetCoreHal.Example.Model.Users.ViewModels;
using AspnetCoreHal.Example.Model;
using AspnetCore.Hal.Configuration;
using AspnetCore.Hal;
using AspnetCore.Hal.NewtonsoftHalJsonFormatter;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(DomainProfile));

builder.Services.AddSingleton<IProvideHalTypeConfiguration>(provider => Halconfig.HypermediaConfiguration());


builder.Services.AddHalSupport();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();



public class DomainProfile : Profile
{
    public DomainProfile()
    {
        CreateMap<UserDetails, UserSummary>();
        CreateMap<RoleDetails, Role>();

    }
}

public class Halconfig
{
    public static HalConfiguration HypermediaConfiguration()
    {
        var config = new HalConfiguration();

        config.For<UserSummary>()
            .Links(model => new Link("self", "/users/{id}", "Info").CreateLink(model));

        config.For<PagedList<UserSummary>>()
              .Embeds("users", x => x.Data)
              .Links(
                  (model, ctx) =>
                  LinkTemplates.Users.GetUsersPaged.CreateLink("self", ctx.Request.Query, new { blah = "123" }))
              .Links(
                  (model, ctx) =>
                  LinkTemplates.Users.GetUsersPaged.CreateLink("Next", "next", ctx.Request.Query, new { page = model.PageNumber + 1 }),
                  model => model.PageNumber < model.TotalPages)
              .Links(
                  (model, ctx) =>
                  LinkTemplates.Users.GetUsersPaged.CreateLink("Previous", "prev", ctx.Request.Query, new { page = model.PageNumber - 1 }),
                  model => model.PageNumber > 0);


        config.For<UserDetails>()
              .Embeds("role", model => model.Role)
              .Links(model => LinkTemplates.Users.GetUser.CreateLink("self", model))
              .Links(model => LinkTemplates.Users.GetUser.CreateLink("Edit", "edit", model))
              .Links(model => LinkTemplates.User.ChangeRole.CreateLink(model))
              .Links(model => LinkTemplates.User.Deactivate.CreateLink(model), model => model.Active)
              .Links(model => LinkTemplates.User.Reactivate.CreateLink(model), model => !model.Active);

        config.For<Role>()
            .Links(model => LinkTemplates.Roles.GetRole.CreateLink("self", model));

        config.For<List<Role>>()
              .Links((model, ctx) => LinkTemplates.Roles.GetRolesPaged.CreateLink("self", ctx.Request.Query));

        config.For<RoleDetails>()
              .Links(model => LinkTemplates.Roles.GetRole.CreateLink("self", model))
              .Links(model => LinkTemplates.Roles.GetRole.CreateLink("Edit", "edit", model))
              .Links(model => LinkTemplates.Roles.GetRole.CreateLink("Delete", "delete", model));

        return config;
    }
}
