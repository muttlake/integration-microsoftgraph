
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Integration.MicrosoftGraph.Library.Clients;
using Integration.MicrosoftGraph.Service.Models;
using Integration.MicrosoftGraph.Library.Models;
using System;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Integration.MicrosoftGraph.Service.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InvitationController : Controller
    {
        private static string tenant = ReadAppSettings.tenant;
        private static string clientId = ReadAppSettings.clientId;
        private static string clientSecret = ReadAppSettings.clientSecret;
        private static InvitationClient inviteClient = new InvitationClient(clientId, clientSecret, tenant);

        [HttpGet]
        public async void GetUser()
        {   
            var client = new HttpClient();
            //TODO:: change to live server.
            var SFResult = await client.GetAsync("api/person");
            var SFUsers = JsonConvert.DeserializeObject<List<SalesforceUser>>(await SFResult.Content.ReadAsStringAsync());
            MSGraphClient msclient = new MSGraphClient(clientId, clientSecret, tenant);
            var msusers = await msclient.GetUsers("");
            var ADUsers = JsonConvert.DeserializeObject<List<AzureUser>>(msusers);
            foreach(var adUser in ADUsers)
            {
                foreach(var sfUser in SFUsers)
                {
                    if (sfUser.EMail == adUser.mail)
                    {
                        SFUsers.Remove(sfUser);
                        ADUsers.Remove(adUser);
                    }
                }
            }
            foreach(var adUser in ADUsers)
            {
                // call delete with adUser.id
                await msclient.DeleteUser((adUser.id).ToString());
            }
            foreach(var sfUser in SFUsers)
            {
                
                await inviteClient.InviteUser(sfUser);
                var uid = await msclient.GetUserId(sfUser.EMail);
                // call brandon's add user to group (GetGroupByName("groupName"), userID)
                GroupClient gclient = new GroupClient(clientId, clientSecret, tenant);
                string gid = await gclient.GetGroupByName("Associates");
                if(String.IsNullOrEmpty(gid))
                {
                    var group = new GroupModel();
                    group.description = "Associate Group";
                    group.displayName = "Associate";
                    group.mailEnabled = false;
                    group.mailNickname = "Associate Mail";
                    group.securityEnabled = false;
                    await gclient.CreateGroup(group);

                    gid = await gclient.GetGroupByName("Associates");
                }
                
                await gclient.AddUserToGroup(gid, uid);
            }
        }
    }
}