using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Decals;
using InventorySystem.Items.Firearms.Modules;
using LabApi.Features.Wrappers;
using PlayhousePlugin.External;
using UnityEngine;

namespace PlayhousePlugin.Cosmetics;

public sealed class SprayService
{
    private static readonly MethodInfo? SendImpactDecal = typeof(ImpactEffectsModule).GetMethod("ServerSendImpactDecal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    private readonly string directory; private readonly DonatorRepository donors; private readonly Dictionary<int,int> freeUses=new(); private readonly Dictionary<int,int> donorUses=new();
    private static readonly Dictionary<string,string> Files=new(StringComparer.OrdinalIgnoreCase){{"1","arrowUp.csv"},{"2","arrowDown.csv"},{"3","arrowLeft.csv"},{"4","arrowRight.csv"},{"1a","amogus.csv"},{"2a","foundationLogo.csv"},{"3a","trollface.csv"},{"4a","klpPog.csv"},{"5a","klpLauvHS.csv"},{"hubert","hubertSmall.csv"}};
    public SprayService(string directory,DonatorRepository donors){this.directory=directory;this.donors=donors;}
    public bool Spray(Player player,string code,out string response)
    {
        if(string.IsNullOrWhiteSpace(directory)||!Files.TryGetValue(code,out string file)){response="Unknown spray or spray directory is not configured.";return false;}
        bool donorSpray=code.EndsWith("a",StringComparison.OrdinalIgnoreCase);int limit=2;
        if(donorSpray){if(!donors.TryGet(player.UserId,out Donator donor)||donor.Tier<1){response="This spray requires donor status.";return false;}limit=donor.Tier;if(donorUses.TryGetValue(player.PlayerId,out int used)&&used>=limit){response="You have used your donor sprays for this round.";return false;}}
        else if(code!="hubert"&&freeUses.TryGetValue(player.PlayerId,out int used)&&used>=limit){response="You have used both free sprays for this round.";return false;}
        string path=Path.Combine(directory,file);if(!File.Exists(path)){response=$"Spray file missing: {path}";return false;}
        if(player.CurrentItem is not FirearmItem firearm){response="Hold a firearm while placing a spray.";return false;}ImpactEffectsModule? impact=firearm.Modules.OfType<ImpactEffectsModule>().FirstOrDefault();if(impact is null){response="This firearm cannot place decals.";return false;}
        Ray initial=new(player.Camera.position,player.Camera.forward);if(!Physics.Raycast(initial,out RaycastHit hit,100)){response="Aim at a nearby surface.";return false;}List<Point> points=Load(path);Vector3 right=player.Camera.right*.05f;Vector3 up=-Vector3.Cross(player.Camera.right,hit.normal)*.05f;int placed=0;
        if(SendImpactDecal is null){response="The current game version does not expose decal placement.";return false;}foreach(Point p in points){Ray ray=new(hit.point+hit.normal+right*p.X+up*p.Y,-hit.normal);if(Physics.Raycast(ray,out RaycastHit pixel,2)){SendImpactDecal.Invoke(impact,new object[]{pixel,ray.origin,DecalPoolType.Bullet});placed++;}}
        if(donorSpray)donorUses[player.PlayerId]=donorUses.TryGetValue(player.PlayerId,out int d)?d+1:1;else freeUses[player.PlayerId]=freeUses.TryGetValue(player.PlayerId,out int f)?f+1:1;response=$"Placed spray ({placed} pixels).";return true;
    }
    private static List<Point> Load(string path){var list=new List<Point>();foreach(string line in File.ReadLines(path)){string[] v=line.Split(',');if(v.Length>=2&&int.TryParse(v[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out int x)&&int.TryParse(v[1],NumberStyles.Integer,CultureInfo.InvariantCulture,out int y))list.Add(new Point(x,y));}return list;}
    public void Reset(){freeUses.Clear();donorUses.Clear();}
    public static string List=>"Sprays: 1 up, 2 down, 3 left, 4 right; donor: 1a Amogus, 2a Foundation, 3a Trollface, 4a Pog, 5a Lauv.";
    private readonly struct Point{public Point(int x,int y){X=x;Y=y;}public int X{get;}public int Y{get;}}
}
