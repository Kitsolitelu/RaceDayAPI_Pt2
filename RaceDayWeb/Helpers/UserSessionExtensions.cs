using System.Text.Json;
using RaceDayWeb.Models;

namespace RaceDayWeb.Helpers
{
    // small helpers to keep the logged in user in the session
    public static class UserSessionExtensions
    {
        private const string UserKey = "CurrentUser";

        public static SessionUser? GetUser(this ISession session)
        {
            var json = session.GetString(UserKey);
            return json == null ? null : JsonSerializer.Deserialize<SessionUser>(json);
        }

        public static void SetUser(this ISession session, SessionUser user)
        {
            session.SetString(UserKey, JsonSerializer.Serialize(user));
        }
    }
}
