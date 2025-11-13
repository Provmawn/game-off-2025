using UnityEngine;

public interface IScannable
{
    void OnScanned();
    string GetScanInfo();
    ScanType GetScanType();
}

public enum ScanType
{
    Item,
    Environment,
    Enemy,
    Hazard,
    Interactive,
    Quest,
    Unknown
}