using ArcticFox.Codec;
using ArcticFox.Codec.Binary;
using ArcticFox.Net;
using ArcticFox.Net.Sockets;
using ProtoBuf;
using ProtoBuf.Meta;
using PvZOL.Protocol.Cmd;
using PvZOL.Protocol.Cmd.Types;
using PvZOL.Protocol.TConnD;

namespace PvZOL.GameServer
{
    public class PvzSocket : HighLevelSocket, ISpanConsumer<byte>
    {
        private static readonly RuntimeTypeModel PvzTypeModel;

        static PvzSocket()
        {
            PvzTypeModel = RuntimeTypeModel.Create();
            // the as3 impl assumes anything unspecified is null
            PvzTypeModel.UseImplicitZeroDefaults = false;
        }
        
        public PvzSocket(SocketInterface socket) : base(socket)
        {
            m_netInputCodec = new CodecChain()
                .AddCodec(new PvzBufferCodec())
                .AddCodec(this);
        }

        public void Input(ReadOnlySpan<byte> input, ref object? state)
        {
            var pkgReader = new BitReader(input);
            var pkg = new TWebPvzPkg();
            pkg.Read(ref pkgReader);

            if (pkgReader.m_dataOffset != pkgReader.m_dataLength)
            {
                throw new InvalidDataException("pkg had trailing data");
            }
            
            var decryptedBody = pkg.Body.ToArray();
            for (var i = 0; i < decryptedBody.Length; i++)
            {
                decryptedBody[i] ^= 7;
            }
            
            var common = PvzTypeModel.Deserialize<CmdCommon>(decryptedBody);
            
            var cmd = CmdFactory.CreateMessage(common.m_cmdName);
            PvzTypeModel.Deserialize(cmd.GetType(), common.m_cmdData, cmd);
            
            Console.Out.WriteLine(cmd);

            if (cmd is Cmd_CheckOldUser_CS)
            {
                EnqueueSend(new Cmd_CheckOldUser_SC 
                {
                    m_isOldUser = true,
                    m_roleName = "no idea",
                    m_qqName = "Socket User"
                });
            } else if (cmd is Cmd_Init_CS)
            {
                EnqueueSend(new Cmd_Init_SC
                {
                    m_latestTDLevel = new Dto_TDLevelInfo
                    {
                        m_stageId = 1,
                        m_levelId = 1,
                        m_subLevelId = 1
                    },
                    m_serverConfig = new List<Dto_ServerConfig>
                    {
                        new Dto_ServerConfig
                        {
                            m_id = "iactivelimitgrade",
                            m_value = "999"
                        }
                    }
                });
            } else if (cmd is Cmd_Idle_CS idle) EnqueueSend(new Cmd_Idle_SC
            {
                m_seqID = idle.m_seqID,
                m_serverTime = 0
            });
            
            else if (cmd is Cmd_SignIn_GetInfo_CS) EnqueueSend(new Cmd_SignIn_GetInfo_SC
            {
                m_signInInfo = new Dto_SignIn_Info
                {
                    m_bIsSigned = true,
                    m_remainSignNum = 99,
                    m_totalSignNum = 100,
                }
            });
            else if (cmd is Cmd_Battle_GetFormation_CS) EnqueueSend(new Cmd_Battle_GetFormation_SC
            {
                m_team = new List<Dto_FormationInfo>
                {
                    new Dto_FormationInfo()
                }
            });
            else if (cmd is Cmd_RoleGuildInfo_CS) EnqueueSend(new Cmd_RoleGuildInfo_SC
            {
                m_roleGuildInfo = new Dto_Role_GuildInfo()
            });
            else if (cmd is Cmd_City_Init_CS) EnqueueSend(new Cmd_City_Init_SC
            {
                m_rolePos = new Dto_WorldMap_Pos()
            });
            
            else if (cmd is Cmd_TD_RequestTDInfo_CS infoRequest) EnqueueSend(new Cmd_TD_RequestTDInfo_SC
            {
                m_stageId = infoRequest.m_stageId,
                m_levelId = infoRequest.m_levelId,
                m_subLevelId = infoRequest.m_subLevelId
            });
            else if (cmd is Cmd_TD_PrivilegeInfo_CS privilegeRequest) EnqueueSend(new Cmd_TD_PrivilegeInfo_SC
            {
                m_stageId = privilegeRequest.m_stageId,
                m_levelId = privilegeRequest.m_levelId,
                m_subLevelId = privilegeRequest.m_subLevelId,
                m_hasPrivilege = true
            });
            else if (cmd is Cmd_TD_GetStageInfo_CS stageInfo) EnqueueSend(new Cmd_TD_GetStageInfo_SC
            {
                m_stageId = stageInfo.m_stageId,
                m_levelInfoList = new List<Dto_TD_LevelInfo>
                {
                    // beaten levels...?
                    // level 6 isn't here but 7 is, therefore 6 is playable? idk
                    new Dto_TD_LevelInfo
                    {
                        m_levelId = 1,
                        m_subLevelId = 1
                    },
                    new Dto_TD_LevelInfo
                    {
                        m_levelId = 3,
                        m_subLevelId = 1
                    },
                    new Dto_TD_LevelInfo
                    {
                        m_levelId = 5,
                        m_subLevelId = 1
                    },
                    new Dto_TD_LevelInfo
                    {
                        m_levelId = 7,
                        m_subLevelId = 1
                    },
                    new Dto_TD_LevelInfo
                    {
                        m_levelId = 10,
                        m_subLevelId = 1
                    }
                }
            });
            
            // let's just respond to everything so we can boot
            else if (cmd is Cmd_Card_GetPackage_CS) EnqueueSend(new Cmd_Card_GetPackage_SC());
            else if (cmd is Cmd_VipInfo_CS) EnqueueSend(new Cmd_VipInfo_SC());
            else if (cmd is Cmd_Arena_GetMyInfo_CS) EnqueueSend(new Cmd_Arena_GetMyInfo_SC());
            else if (cmd is Cmd_Item_GetBagInfo_CS) EnqueueSend(new Cmd_Item_GetBagInfo_SC());
            else if (cmd is Cmd_Quest_GetList_CS) EnqueueSend(new Cmd_Quest_GetList_SC());
            else if (cmd is Cmd_DailyQuest_GetInfo_CS) EnqueueSend(new Cmd_DailyQuest_GetInfo_SC());
            else if (cmd is Cmd_DailyActivity_GetInfo_CS) EnqueueSend(new Cmd_DailyActivity_GetInfo_SC());
            else if (cmd is Cmd_City_GetBuildingInfo_CS) EnqueueSend(new Cmd_City_GetBuildingInfo_SC());
            else if (cmd is Cmd_Flag_GetList_CS) EnqueueSend(new Cmd_Flag_GetList_SC());
            else if (cmd is Cmd_Lab_GetLabInfo_CS) EnqueueSend(new Cmd_Lab_GetLabInfo_SC());
            else if (cmd is Cmd_War_GetDeclareWarStat_CS) EnqueueSend(new Cmd_War_GetDeclareWarStat_SC());
            else if (cmd is Cmd_Setout_GetSetoutTimes_CS) EnqueueSend(new Cmd_Setout_GetSetoutTimes_SC());
            else if (cmd is Cmd_Buff_GetList_CS) EnqueueSend(new Cmd_Buff_GetList_SC());
            else if (cmd is Cmd_Chat_GetHistory_CS) EnqueueSend(new Cmd_Chat_GetHistory_SC());
            else if (cmd is Cmd_Friend_GetBlackList_CS) EnqueueSend(new Cmd_Friend_GetBlackList_SC());
            else if (cmd is Cmd_FlexibleActivity_GetActivityList_CS) EnqueueSend(new Cmd_FlexibleActivity_GetActivityList_SC());
            else if (cmd is Cmd_OnlineReward_GetInfo_CS) EnqueueSend(new Cmd_OnlineReward_GetInfo_SC());
            else if (cmd is Cmd_PayShop_GetFlagList_CS) EnqueueSend(new Cmd_PayShop_GetFlagList_SC());
            else if (cmd is Cmd_PVZPrivilege_CS) EnqueueSend(new Cmd_PVZPrivilege_SC());
            else if (cmd is Cmd_Fuben_GetChapterInfo_CS) EnqueueSend(new Cmd_Fuben_GetChapterInfo_SC());
            else if (cmd is Cmd_GetLevelRewardInfo_CS) EnqueueSend(new Cmd_GetLevelRewardInfo_SC());
            else if (cmd is Cmd_City_MergeServerFlag_CS) EnqueueSend(new Cmd_City_MergeServerFlag_SC());
            else if (cmd is Cmd_Talent_GetList_CS) EnqueueSend(new Cmd_Talent_GetList_SC());
            else if (cmd is Cmd_Puppet_GetInfo_CS) EnqueueSend(new Cmd_Puppet_GetInfo_SC());
            else if (cmd is Cmd_Puppet_GetFormation_CS) EnqueueSend(new Cmd_Puppet_GetFormation_SC());
            else if (cmd is Cmd_PayShop_GetDayTicketList_CS) EnqueueSend(new Cmd_PayShop_GetDayTicketList_SC());
            else if (cmd is Cmd_PayShop_GetGiftTicketList_CS) EnqueueSend(new Cmd_PayShop_GetGiftTicketList_SC());
            else if (cmd is Cmd_FlexibleActivity_GetActivityList_CS) EnqueueSend(new Cmd_FlexibleActivity_GetActivityList_SC());
            else if (cmd is Cmd_Guide_GetKeys_CS) EnqueueSend(new Cmd_Guide_GetKeys_SC());
            else if (cmd is Cmd_SecondPwd_Info_CS) EnqueueSend(new Cmd_SecondPwd_Info_SC());
            else if (cmd is Cmd_Notice_GetRollList_CS) EnqueueSend(new Cmd_Notice_GetRollList_SC());
            else if (cmd is Cmd_Notice_GetSysList_CS) EnqueueSend(new Cmd_Notice_GetSysList_SC());
        }

        private void EnqueueSend(object cmd)
        {
            m_taskQueue.Enqueue(() => Send(cmd));
        }

        private ValueTask Send(object cmd)
        {
            var cmdStream = new MemoryStream();
            PvzTypeModel.Serialize(cmdStream, cmd);
            cmdStream.Flush();
            
            var cmdCommon = new CmdCommon
            {
                m_cmdName = $"PVZ.Cmd.{cmd.GetType().Name}", // todo: ...
                m_cmdData = cmdStream.ToArray(),
                m_seqId = 0, // todo: does it matter. init reads at least
            };

            var cmdCommonStream = new MemoryStream();
            PvzTypeModel.Serialize(cmdCommonStream, cmdCommon);
            cmdCommonStream.Flush();

            var pkg = new TWebPvzPkg
            {
                Head = new TWebBase
                {
                    Cmd = 3, // TWEB_CMD_DOWNLOAD (presumably)
                },
                Body = cmdCommonStream.ToArray()
            };

            var wireWriter = new GrowingBitWriter();
            pkg.Write(ref wireWriter);

            var task = this.BroadcastBytes(wireWriter.GetData().Span);
            wireWriter.Dispose(); // todo: ew
            return task;
        }

        public void Abort()
        {
            Close();
        }
    }
}