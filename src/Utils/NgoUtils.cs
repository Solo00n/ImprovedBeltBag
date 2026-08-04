using Unity.Netcode;

namespace ImprovedBeltBag.Utils
{
    internal static class NgoUtils
    {
        /// <summary>
        /// True only while the SERVER is executing a received *ServerRpc (not while a client is
        /// sending one). In this game's Netcode the exec-stage enum value 1 == Execute/Server and
        /// 0 == Send/None, so we compare numerically to stay robust across enum renames.
        /// </summary>
        internal static bool IsRpcServerStage(this NetworkBehaviour self)
        {
            var nm = self.NetworkManager;
            if (nm == null || !nm.IsListening) return false;
            if ((int)self.__rpc_exec_stage != 1) return false;
            return nm.IsServer || nm.IsHost;
        }
    }
}
