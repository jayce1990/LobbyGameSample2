
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;
using VivoxUnity;

namespace vivox
{
    //在大厅内设置语音频道
    public class VivoxSetup
    {
        private bool m_hasInitialized = false;
        private bool m_isMidInitialize = false;
        private ILoginSession m_loginSession = null;
        private IChannelSession m_channelSession = null;
        private List<VivoxPlayerHandler> m_playerHandlers;

        //在GameManager.Awake(实际加入任何语音频道前),初始化Vivox服务.
        public void Initialize(List<VivoxPlayerHandler> playerHandlers, Action<bool> onComplete)
        {
            if (m_isMidInitialize)
                return;
            m_isMidInitialize = true;

            m_playerHandlers = playerHandlers;

            VivoxService.Instance.Initialize();

            m_loginSession = VivoxService.Instance.Client.GetLoginSession(new Account(AuthenticationService.Instance.PlayerId));
            m_loginSession.BeginLogin(m_loginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, result => 
            {
                try
                {
                    m_loginSession.EndLogin(result);
                    m_hasInitialized = true;
                    onComplete?.Invoke(true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Vivox failed to login: " + ex.Message);
                    onComplete?.Invoke(false);
                }
                finally
                {
                    m_isMidInitialize = false;
                }
            });
        }

        /// <summary>
        /// 一进入大厅,就开始加入那个大厅的语音频道.首先确认已经Initialize了.
        /// </summary>        
        /// <param name="onComplete">在频道加入成功或失败时调用</param>
        public void JoinLobbyChannel(string lobbyId, Action<bool> onComplete)
        {
            if (!m_hasInitialized || m_loginSession.State != LoginState.LoggedIn)
            {
                Debug.LogWarning("Vivox还没有登录完成,不能加入Vivox语音频道.");
                onComplete?.Invoke(false);
                return;
            }

            Channel channel = new Channel(lobbyId + "_voice", ChannelType.NonPositional, null);
            m_channelSession = m_loginSession.GetChannelSession(channel);
            string token = m_channelSession.GetConnectToken();

            m_channelSession.BeginConnect(true, false, true, token, result => {
                try
                {
                    //可能在连接过程中又离开大厅,断开了连接,直接返回.
                    if (m_channelSession.ChannelState == ConnectionState.Disconnecting || m_channelSession.ChannelState == ConnectionState.Disconnected)
                    {
                        Debug.LogWarning("Vivox连接已经断开连接,中止频道连接后续.");
                        LeaveLobbyChannel();
                        return;
                    }

                    m_channelSession.EndConnect(result);
                    onComplete?.Invoke(true);
                    foreach (VivoxPlayerHandler playerHandler in m_playerHandlers)
                        playerHandler.OnChannelJoined(m_channelSession);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Vivox连接失败:" + ex.Message);
                    onComplete?.Invoke(false);
                    m_channelSession?.Disconnect();
                }
            });
        }

        public async void LeaveLobbyChannel()//离开大厅时调用
        {
            if (m_channelSession != null)
            {
                //等待连接完成后,再调用断开连接.
                if (m_channelSession.ChannelState == ConnectionState.Connecting)
                {
                    while (m_channelSession?.ChannelState == ConnectionState.Connecting)
                    {
                        await Task.Delay(200);
                    }
                    LeaveLobbyChannel();
                    return;
                }

                m_channelSession?.Disconnect((result)=> 
                {
                    m_loginSession.DeleteChannelSession(m_channelSession.Channel);
                    m_channelSession = null;
                });
            }

            foreach (VivoxPlayerHandler playerHandler in m_playerHandlers)
                playerHandler.OnChannelLeft();
        }

        //退出游戏时调用,这将时玩家与Vivox完全断开连接,而不仅仅是离开任何开发的大厅频道.
        public void Uninitialize()
        {
            if (!m_hasInitialized)
                return;

            m_loginSession.Logout();
        }
    }
}