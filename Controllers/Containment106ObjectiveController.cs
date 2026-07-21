using System;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using PlayhousePlugin.Integrations;
using PlayhousePlugin.Runtime;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace PlayhousePlugin.Controllers;

public sealed class Containment106ObjectiveController : IDisposable
{
    private readonly SchematicService schematics; private readonly PluginRuntime runtime;
    private SchematicObject? terminal; private Pickup? button; private ScheduledHandle? loop; private Vector3 position; private float progress; private Stage stage;
    public bool Allow106Containment => stage == Stage.Armed;
    public Containment106ObjectiveController(SchematicService schematics,PluginRuntime runtime){this.schematics=schematics;this.runtime=runtime;}
    public void Spawn(){Destroy();Room? room=Room.List.FirstOrDefault(r=>r.Name==RoomName.Hcz106);if(room is null)return;position=room.Transform.TransformPoint(new Vector3(25.7f,1,-13.2f));Quaternion rotation=room.Rotation*Quaternion.Euler(0,90,0);terminal=schematics.Spawn("Terminal",position,rotation,Vector3.one);if(terminal==null)return;Vector3 buttonPos=terminal.gameObject.transform.TransformPoint(new Vector3(-.116f,.237f,.073f));button=Pickup.Create(ItemType.ArmorLight,buttonPos,rotation*Quaternion.Euler(0,-90,90),new Vector3(.12f,.12f,.08f));if(button is null)return;button.Spawn();button.IsLocked=false;if(button.Rigidbody is not null){button.Rigidbody.useGravity=false;button.Rigidbody.isKinematic=true;}loop=runtime.Repeat(.25f,Tick);}
    public void OnPickingUpItem(PlayerPickingUpItemEventArgs ev){if(button is null||ev.Pickup.Serial!=button.Serial)return;ev.IsAllowed=false;if(stage==Stage.Armed){Recontain(ev.Player);return;}if(stage!=Stage.Disabled){ev.Player.SendHint($"<color=yellow>Containment preparation {progress/2:0}%</color>",3);return;}if(!Generator.List.Any(g=>g.Engaged)){ev.Player.SendHint("<color=red>One SCP-079 generator must be engaged first.</color>",3);return;}if(!ev.Player.IsHuman){ev.Player.SendHint("<color=red>Only human personnel can operate this terminal.</color>",3);return;}stage=Stage.Capturing;ev.Player.SendHint("<color=yellow>SCP-106 containment preparation started.</color>",3);}
    private void Tick(){if(stage!=Stage.Capturing)return;Player[] near=Player.ReadyList.Where(p=>p.IsAlive&&Vector3.Distance(p.Position,position)<=5).ToArray();int humans=near.Count(p=>p.IsHuman),scps=near.Count(p=>p.Team==Team.SCPs);if(humans>0&&scps==0)progress+=(float)Math.Log(humans*humans)+1.5f;else if(humans==0)progress=Math.Max(0,progress-.5f);foreach(Player p in near)p.SendHint(scps>0?"<color=grey>Containment objective contested</color>":$"SCP-106 preparation {progress/2:0}%",.4f);foreach(Player scp in Player.ReadyList.Where(p=>p.Role==RoleTypeId.Scp106))scp.SendHint("<color=yellow>You sense activity near your containment chamber...</color>",1);if(progress>=200){stage=Stage.Armed;Announcer.Message("SCP 1 0 6 recontainment procedure initiated waiting for manual reactivation",string.Empty,true,0,1);}else if(progress<=0)stage=Stage.Disabled;}
    private void Recontain(Player activator){Player[] targets=Player.ReadyList.Where(p=>p.Role==RoleTypeId.Scp106).ToArray();if(targets.Length==0){activator.SendHint("<color=yellow>No active SCP-106 instance.</color>",3);return;}foreach(Player target in targets)target.Kill("SCP-106 recontainment procedure",string.Empty);stage=Stage.Complete;Announcer.Message("SCP 1 0 6 successfully recontained",string.Empty,true,0,1);activator.SendHint("<color=green>SCP-106 recontained.</color>",4);}
    public void Destroy(){loop?.Dispose();loop=null;if(button is not null&&!button.IsDestroyed)button.Destroy();button=null;schematics.Destroy(terminal);terminal=null;progress=0;stage=Stage.Disabled;}public void Dispose()=>Destroy();
    private enum Stage{Disabled,Capturing,Armed,Complete}
}
