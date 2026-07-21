using System;
using System.Collections.Generic;
using CommandSystem;
using LabApi.Features.Wrappers;
using PlayhousePlugin.Cosmetics;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class PetsCommand : ICommand
{
    public string Command=>"pets";public string[] Aliases=>new[]{"pet"};public string Description=>"Lists, equips, or removes a donor pet.";
    public bool Execute(ArraySegment<string> a,ICommandSender s,out string response){Player? p=Player.Get(s);if(p is null){response="Player only.";return false;}CosmeticService? c=PlayhousePlugin.Instance?.Cosmetics;if(c is null){response="Cosmetics unavailable.";return false;}if(a.Count==0){response=CosmeticService.PetList;return true;}string code=a.At(0);if(code is "0" or "remove" or "unequip"){c.Remove(p,true);response="Pet removed.";return true;}return c.EquipPet(p,code,out response);}
}
[CommandHandler(typeof(ClientCommandHandler))]
public sealed class HatsCommand : ICommand
{
    private static readonly Dictionary<string,string> Names=new(StringComparer.OrdinalIgnoreCase){{"1a","Egg"},{"1b","Frog"},{"1c","Halo"},{"1d","Horns"}};
    public string Command=>"hats";public string[] Aliases=>new[]{"hat"};public string Description=>"Lists, equips, or removes a cosmetic hat.";
    public bool Execute(ArraySegment<string>a,ICommandSender s,out string response){Player? p=Player.Get(s);PlayhousePlugin? plugin=PlayhousePlugin.Instance;if(p is null||plugin?.Cosmetics is null){response="Player only.";return false;}if(!plugin.Config.Cosmetics.EnableHats){response="Hats are disabled on this server.";return false;}if(a.Count==0){response="Hats: 1a Egg, 1b Frog, 1c Halo, 1d Horns";return true;}string code=a.At(0);if(code is "0" or "remove" or "unequip"){plugin.Cosmetics.RemoveHat(p.UserId);response="Hat removed.";return true;}if(!Names.TryGetValue(code,out string name)){response="Unknown hat code.";return false;}return plugin.Cosmetics.EquipHat(p,name,out response);}
}
