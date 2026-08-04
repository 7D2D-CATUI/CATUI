using HarmonyLib;

[HarmonyPatch]
public class XUiC_PlayersListPatch
{
	// 连接服务器/下载世界阶段，World 尚未就绪时，好友/盟友状态更新包(NetPackageAllyResponse)
	// 会触发 XUiC_PlayersList.updatePlayersList()，其中直接解引用 GameManager.Instance.World
	// (见 vanilla XUiC_PlayersList.cs 行 124/135/171)，产生 NullReferenceException。
	// 此补丁在世界/持久玩家数据未就绪时跳过刷新，世界加载完成后恢复正常行为。
	[HarmonyPatch(typeof(XUiC_PlayersList), "updatePlayersList")]
	[HarmonyPrefix]
	public static bool UpdatePlayersListPrefix()
	{
		if (GameManager.Instance == null || GameManager.Instance.World == null || GameManager.Instance.persistentPlayers == null)
		{
			return false;
		}
		return true;
	}
}
