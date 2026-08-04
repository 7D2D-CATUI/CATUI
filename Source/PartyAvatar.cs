using System;
using System.Collections.Generic;
using Platform.Steam;
using Steamworks;
using UnityEngine;

public static class PartyAvatar
{
	private static readonly Dictionary<ulong, Texture2D> cache = new Dictionary<ulong, Texture2D>();

	private static readonly Dictionary<ulong, List<Action<Texture2D>>> pending = new Dictionary<ulong, List<Action<Texture2D>>>();

	private static Callback<AvatarImageLoaded_t> avatarLoadedCallback;

	private static bool callbackRegistered;

	public static ulong GetSteamId(EntityPlayer player)
	{
		try
		{
			if (player == null)
			{
				return 0UL;
			}
			PersistentPlayerData persistentPlayerData = player.PersistentPlayerData;
			if (persistentPlayerData == null)
			{
				return 0UL;
			}
			// 优先使用原生平台标识(Steam)，跨平台(EOS)玩家拿不到 SteamId 时返回 0
			if (persistentPlayerData.NativeId is UserIdentifierSteam nativeSteam)
			{
				return nativeSteam.SteamId;
			}
			if (persistentPlayerData.PrimaryId is UserIdentifierSteam primarySteam)
			{
				return primarySteam.SteamId;
			}
		}
		catch (Exception e)
		{
			Log.Error("[CATUI] PartyAvatar.GetSteamId error: " + e);
		}
		return 0UL;
	}

	public static void RequestAvatar(ulong steamId, Action<Texture2D> onDone)
	{
		if (steamId == 0UL || onDone == null)
		{
			return;
		}
		if (cache.TryGetValue(steamId, out Texture2D cached))
		{
			onDone(cached);
			return;
		}
		if (!IsSteamReady())
		{
			return;
		}
		int handle = SteamFriends.GetMediumFriendAvatar(new CSteamID(steamId));
		if (handle > 0)
		{
			Texture2D texture = BuildTexture(steamId, handle);
			if (texture != null)
			{
				onDone(texture);
			}
			return;
		}
		// 头像尚未加载完成，等待 AvatarImageLoaded_t 回调
		RegisterCallback();
		if (!pending.TryGetValue(steamId, out List<Action<Texture2D>> list))
		{
			list = new List<Action<Texture2D>>();
			pending[steamId] = list;
		}
		list.Add(onDone);
	}

	private static bool IsSteamReady()
	{
		try
		{
			return SteamAPI.IsSteamRunning();
		}
		catch (Exception e)
		{
			Log.Error("[CATUI] PartyAvatar.IsSteamReady error: " + e);
			return false;
		}
	}

	private static void RegisterCallback()
	{
		if (callbackRegistered)
		{
			return;
		}
		callbackRegistered = true;
		avatarLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
	}

	private static void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
	{
		ulong steamId = callback.m_steamID.m_SteamID;
		if (!pending.TryGetValue(steamId, out List<Action<Texture2D>> list))
		{
			return;
		}
		pending.Remove(steamId);
		if (callback.m_iImage <= 0)
		{
			return;
		}
		Texture2D texture = BuildTexture(steamId, callback.m_iImage);
		if (texture == null)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			try
			{
				list[i]?.Invoke(texture);
			}
			catch (Exception e)
			{
				Log.Error("[CATUI] PartyAvatar.OnAvatarImageLoaded callback error: " + e);
			}
		}
	}

	private static Texture2D BuildTexture(ulong steamId, int handle)
	{
		try
		{
			if (cache.TryGetValue(steamId, out Texture2D existing))
			{
				return existing;
			}
			if (!SteamUtils.GetImageSize(handle, out uint width, out uint height) || width == 0 || height == 0)
			{
				return null;
			}
			byte[] rgba = new byte[width * height * 4];
			if (!SteamUtils.GetImageRGBA(handle, rgba, rgba.Length))
			{
				return null;
			}
			// Steam 图像自上而下存储，Unity 纹理自下而上，垂直翻转避免头像倒立
			int stride = (int)width * 4;
			byte[] flipped = new byte[rgba.Length];
			for (uint y = 0; y < height; y++)
			{
				Buffer.BlockCopy(rgba, (int)((height - 1 - y) * stride), flipped, (int)(y * stride), stride);
			}
			Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			texture.LoadRawTextureData(flipped);
			texture.Apply();
			texture.hideFlags = HideFlags.HideAndDontSave;
			cache[steamId] = texture;
			return texture;
		}
		catch (Exception e)
		{
			Log.Error("[CATUI] PartyAvatar.BuildTexture error: " + e);
			return null;
		}
	}
}
