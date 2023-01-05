using Nox.Core.Configuration;

namespace Nox.Cli.Helpers;

public static class TeamMemberHelper
{
    public static List<TeamMemberConfiguration> ToTeamMemberConfiguration(this List<object> teamMemberDictionary)
    {
        var result = new List<TeamMemberConfiguration>();
        foreach (Dictionary<object, object> item in teamMemberDictionary)
        {
            var name = item.GetValueOrDefault("name")?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) throw new MissingFieldException("Team member name cannot be empty");
            var userName = item.GetValueOrDefault("userName")?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(userName)) throw new MissingFieldException("Team member user name cannot be empty");
            var isAdmin = (item.GetValueOrDefault("isAdmin")?.ToString() ?? "") == "true";
            
            result.Add(new TeamMemberConfiguration
            {
                Name = name,
                UserName = userName,
                IsAdmin = isAdmin
            });
        }
        return result;
    }
}