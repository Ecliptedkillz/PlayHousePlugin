using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using PlayhousePlugin.Cosmetics;
namespace PlayhousePlugin.Commands;
[CommandHandler(typeof(ClientCommandHandler))]
public sealed class SprayCommand:ICommand{public string Command=>"spray";public string[] Aliases=>new[]{"sprays"};public string Description=>"Lists or places a spray decal.";public bool Execute(ArraySegment<string>a,ICommandSender s,out string r){Player? p=Player.Get(s);SprayService? service=PlayhousePlugin.Instance?.Sprays;if(p is null||service is null){r="Sprays unavailable.";return false;}if(a.Count==0){r=SprayService.List;return true;}return service.Spray(p,a.At(0),out r);}}
