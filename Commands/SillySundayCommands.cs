using System;
using CommandSystem;
using LabApi.Features.Permissions;

namespace PlayhousePlugin.Commands;
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SillySundayCommand:ICommand{public string Command=>"sillysunday";public string[] Aliases=>Array.Empty<string>();public string Description=>"Toggles Silly Sunday.";public bool Execute(ArraySegment<string>a,ICommandSender s,out string r){if(!s.HasPermission("at.sillysunday")){r="Missing permission: at.sillysunday";return false;}var service=PlayhousePlugin.Instance?.SillySunday;if(service is null){r="Unavailable.";return false;}service.IsEnabled=!service.IsEnabled;if(!service.IsEnabled)service.Reset();r=$"Silly Sunday {(service.IsEnabled?"enabled":"disabled")}.";return true;}}
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SundayEventsCommand:ICommand{public string Command=>"sundayevents";public string[] Aliases=>new[]{"se","sevents"};public string Description=>"Starts a Silly Sunday event.";public bool Execute(ArraySegment<string>a,ICommandSender s,out string r){if(!s.HasPermission("at.sundayevents")){r="Missing permission: at.sundayevents";return false;}if(a.Count!=1){r="Usage: sundayevents <event>";return false;}return PlayhousePlugin.Instance?.SillySunday?.Start(a.At(0),out r)??Fail(out r);}private static bool Fail(out string r){r="Unavailable.";return false;}}
