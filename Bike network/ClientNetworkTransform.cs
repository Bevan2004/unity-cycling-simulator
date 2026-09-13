using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTransform : NetworkTransform
{
    // This tells Unity: "The Client (Owner) is the boss of this object's position, not the Server."
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}