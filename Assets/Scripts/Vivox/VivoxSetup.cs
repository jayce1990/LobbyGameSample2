
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;
using VivoxUnity;

namespace vivox
{
    //�ڴ�������������Ƶ��
    public class VivoxSetup
    {
        private bool m_hasInitialized = false;
        private bool m_isMidInitialize = false;
        private ILoginSession m_loginSession = null;
        private IChannelSession m_channelSession = null;
        private List<VivoxPlayerHandler> m_playerHandlers;

        //��GameManager.Awake(ʵ�ʼ����κ�����Ƶ��ǰ),��ʼ��Vivox����.
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
        /// һ�������,�Ϳ�ʼ�����Ǹ�����������Ƶ��.����ȷ���Ѿ�Initialize��.
        /// </summary>        
        /// <param name="onComplete">��Ƶ������ɹ���ʧ��ʱ����</param>
        public void JoinLobbyChannel(string lobbyId, Action<bool> onComplete)
        {
            if (!m_hasInitialized || m_loginSession.State != LoginState.LoggedIn)
            {
                Debug.LogWarning("Vivox��û�е�¼���,���ܼ���Vivox����Ƶ��.");
                onComplete?.Invoke(false);
                return;
            }

            Channel channel = new Channel(lobbyId + "_voice", ChannelType.NonPositional, null);
            m_channelSession = m_loginSession.GetChannelSession(channel);
            string token = m_channelSession.GetConnectToken();

            m_channelSession.BeginConnect(true, false, true, token, result => {
                try
                {
                    //���������ӹ��������뿪����,�Ͽ�������,ֱ�ӷ���.
                    if (m_channelSession.ChannelState == ConnectionState.Disconnecting || m_channelSession.ChannelState == ConnectionState.Disconnected)
                    {
                        Debug.LogWarning("Vivox�����Ѿ��Ͽ�����,��ֹƵ�����Ӻ���.");
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
                    Debug.LogWarning("Vivox����ʧ��:" + ex.Message);
                    onComplete?.Invoke(false);
                    m_channelSession?.Disconnect();
                }
            });
        }

        public async void LeaveLobbyChannel()//�뿪����ʱ����
        {
            if (m_channelSession != null)
            {
                //�ȴ�������ɺ�,�ٵ��öϿ�����.
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

        //�˳���Ϸʱ����,�⽫ʱ�����Vivox��ȫ�Ͽ�����,�����������뿪�κο����Ĵ���Ƶ��.
        public void Uninitialize()
        {
            if (!m_hasInitialized)
                return;

            m_loginSession.Logout();
        }
    }
}