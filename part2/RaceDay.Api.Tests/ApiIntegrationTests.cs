using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public ApiIntegrationTests(CustomWebApplicationFactory factory)=>_factory=factory;
    private HttpClient Client()=>_factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false,HandleCookies=true});
    private static async Task Login(HttpClient c,string email)=>Assert.Equal(HttpStatusCode.OK,(await c.PostAsJsonAsync("/api/auth/login",new{email,password="Password1!"})).StatusCode);

    [Fact] public async Task Register_ValidParticipant_ReturnsCreated(){var c=Client();var r=await c.PostAsJsonAsync("/api/auth/register",new{firstName="Joy",lastName="Mafalo",email=$"joy{Guid.NewGuid()}@test.com",password="StrongPass1!",roleName="Participant"});Assert.Equal(HttpStatusCode.Created,r.StatusCode);}
    [Fact] public async Task Register_DuplicateEmail_ReturnsConflict(){var c=Client();var body=new{firstName="New",lastName="User",email="participant@raceday.test",password="StrongPass1!",roleName="Participant"};Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/auth/register",body)).StatusCode);}
    [Fact] public async Task Login_WrongPassword_ReturnsUnauthorized(){var c=Client();Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsJsonAsync("/api/auth/login",new{email="participant@raceday.test",password="wrong"})).StatusCode);}
    [Fact] public async Task Profile_WithoutSession_ReturnsUnauthorized(){var c=Client();Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/users/me")).StatusCode);}
    [Fact] public async Task Participant_CannotCreateEvent(){var c=Client();await Login(c,"participant@raceday.test");var r=await c.PostAsJsonAsync("/api/events",EventBody());Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode);}
    [Fact] public async Task Organiser_CanCreateAndReadEvent(){var c=Client();await Login(c,"organiser@raceday.test");var created=await c.PostAsJsonAsync("/api/events",EventBody());Assert.Equal(HttpStatusCode.Created,created.StatusCode);var item=await created.Content.ReadFromJsonAsync<IdResponse>();Assert.Equal(HttpStatusCode.OK,(await c.GetAsync($"/api/events/{item!.eventID}")).StatusCode);}
    [Fact] public async Task Public_CanListEvents(){var c=Client();Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/events")).StatusCode);}
    [Fact] public async Task Organiser_CanUpdateAndDeleteOwnEvent(){var c=Client();await Login(c,"organiser@raceday.test");var ev=await(await c.PostAsJsonAsync("/api/events",EventBody())).Content.ReadFromJsonAsync<IdResponse>();var update=new{eventName="Updated Heritage Run",description="Updated test event",eventDate=DateTime.UtcNow.AddDays(40),location="Polokwane",distance=21.1,eventType="Run"};Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync($"/api/events/{ev!.eventID}",update)).StatusCode);Assert.Equal(HttpStatusCode.NoContent,(await c.DeleteAsync($"/api/events/{ev.eventID}")).StatusCode);}
    [Fact] public async Task Organiser_CanCreateCategory(){var c=Client();await Login(c,"organiser@raceday.test");var ev=await (await c.PostAsJsonAsync("/api/events",EventBody())).Content.ReadFromJsonAsync<IdResponse>();var r=await c.PostAsJsonAsync($"/api/events/{ev!.eventID}/categories",new{categoryName=$"10km-{Guid.NewGuid()}",categoryType="Distance",description="Ten kilometre category"});Assert.Equal(HttpStatusCode.Created,r.StatusCode);}
    [Fact] public async Task Participant_CanEnrol_AndDuplicateIsRejected(){var setup=Client();await Login(setup,"organiser@raceday.test");var ev=await (await setup.PostAsJsonAsync("/api/events",EventBody())).Content.ReadFromJsonAsync<IdResponse>();var cat=await (await setup.PostAsJsonAsync($"/api/events/{ev!.eventID}/categories",new{categoryName="Open",categoryType="Age"})).Content.ReadFromJsonAsync<CategoryIdResponse>();var participant=Client();await Login(participant,"participant@raceday.test");var first=await participant.PostAsJsonAsync($"/api/events/{ev.eventID}/enrolments",new{categoryId=cat!.categoryID});Assert.Equal(HttpStatusCode.Created,first.StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await participant.PostAsJsonAsync($"/api/events/{ev.eventID}/enrolments",new{categoryId=cat.categoryID})).StatusCode);}
    [Fact] public async Task OrganiserEndpoint_RejectsUnauthenticatedRequest(){var c=Client();Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/organisers/me/events")).StatusCode);}
    [Fact] public async Task Participant_CannotViewOrganiserEventEnrolments(){var c=Client();await Login(c,"participant@raceday.test");Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/api/events/1/enrolments")).StatusCode);}
    [Fact] public async Task Swagger_IsAvailable(){var c=Client();Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/swagger/v1/swagger.json")).StatusCode);}
    [Fact] public async Task Logout_ClearsSession(){var c=Client();await Login(c,"participant@raceday.test");Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/users/me")).StatusCode);await c.PostAsync("/api/auth/logout",null);Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/users/me")).StatusCode);}

    private static object EventBody()=>new{eventName=$"Race {Guid.NewGuid()}",description="Test event",eventDate=DateTime.UtcNow.AddDays(30),location="Thohoyandou",distance=10.0,eventType="Run"};
    private record IdResponse(int eventID);
    private record CategoryIdResponse(int categoryID);
}
