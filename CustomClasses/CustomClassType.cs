namespace PlayhousePlugin.CustomClasses;

public enum CustomClassType
{
    None,
    NtfMedic, ChaosMedic,
    NtfHeavy, ChaosHeavy,
    NtfScout, ChaosScout,
    NtfDemoman, ChaosDemoman,
    NtfDemolitionsExpert, ChaosDemolitionsExpert,
    NtfContainmentSpecialist, ChaosContainmentSpecialist,
    NtfBulldozer, ChaosBulldozer,
    NtfHunter, ChaosHunter,
    NtfExterminator, ChaosExterminator,
    NtfHeretic, ChaosHeretic,
    NtfEngineer, ChaosEngineer,
    NtfMachinist, ChaosMachinist,
    NtfManager, ChaosManager
}

public static class CustomClassTypeResolver
{
    public static CustomClassType Resolve(CustomClassBase customClass) =>
        customClass.Name switch
        {
            "NTF Medic" => CustomClassType.NtfMedic,
            "Chaos Medic" => CustomClassType.ChaosMedic,
            "NTF Heavy" => CustomClassType.NtfHeavy,
            "Chaos Heavy" => CustomClassType.ChaosHeavy,
            "NTF Scout" => CustomClassType.NtfScout,
            "Chaos Scout" => CustomClassType.ChaosScout,
            "NTF Demoman" => CustomClassType.NtfDemoman,
            "Chaos Demoman" => CustomClassType.ChaosDemoman,
            "NTF Demolitions Expert" => CustomClassType.NtfDemolitionsExpert,
            "Chaos Demolitions Expert" => CustomClassType.ChaosDemolitionsExpert,
            "NTF Containment Specialist" => CustomClassType.NtfContainmentSpecialist,
            "Chaos Containment Specialist" => CustomClassType.ChaosContainmentSpecialist,
            "NTF Bulldozer" => CustomClassType.NtfBulldozer,
            "Chaos Bulldozer" => CustomClassType.ChaosBulldozer,
            "NTF Hunter" => CustomClassType.NtfHunter,
            "Chaos Hunter" => CustomClassType.ChaosHunter,
            "NTF Exterminator" => CustomClassType.NtfExterminator,
            "Chaos Exterminator" => CustomClassType.ChaosExterminator,
            "NTF Heretic" => CustomClassType.NtfHeretic,
            "Chaos Heretic" => CustomClassType.ChaosHeretic,
            "NTF Engineer" => CustomClassType.NtfEngineer,
            "Chaos Engineer" => CustomClassType.ChaosEngineer,
            "NTF Machinist" => CustomClassType.NtfMachinist,
            "Chaos Machinist" => CustomClassType.ChaosMachinist,
            "NTF Manager" => CustomClassType.NtfManager,
            "Chaos Manager" => CustomClassType.ChaosManager,
            _ => CustomClassType.None
        };
}