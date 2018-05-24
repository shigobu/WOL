using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WOL
{
    class Program
    {
        static void Main(string[] args)
        {
            string ipAddress = "8.8.8.7";
            byte[] physicalAddress = { 0, 0, 0, 0, 0, 0 };

            //ファイルから、ipアドレスとMACアドレス取得
            //自身のパス取得
            System.Reflection.Assembly assem = typeof(Program).Assembly;
            string assemPath = assem.Location;
            string assemDictionary = Path.GetDirectoryName(assemPath);

            //設定ファイルフルパス作成
            string settingFileName = "Setting.txt";
            string settingFilePath = Path.Combine(assemDictionary, settingFileName);

            //存在確認
            if(!File.Exists(settingFilePath))
            {
                setColorError();
                Console.WriteLine("設定ファイルが見つかりません。");
                Console.WriteLine(settingFilePath);
                System.Threading.Thread.Sleep(2000);
                Console.ResetColor();
                return;
            }

            //ファイルから読み込み
            var sr = new StreamReader(settingFilePath);
            try
            {
                ipAddress = sr.ReadLine();
                string pAddr = sr.ReadLine();
                string[] pAddrArr = pAddr.Split('-');
                for(int i = 0; i < 6; i++)
                {
                    physicalAddress[i] = byte.Parse(pAddrArr[i], System.Globalization.NumberStyles.HexNumber);
                }
            }
            catch (Exception ex)
            {
                setColorError();
                Console.WriteLine("ファイル読み込みに失敗しました。");
                Console.WriteLine(ex.Message);
                System.Threading.Thread.Sleep(2000);
                return;
            }
            finally
            {
                sr.Dispose();
                Console.ResetColor();
            }

            //pingを送信
            Console.WriteLine("pingの送信・・・");
            var p = new System.Net.NetworkInformation.Ping();
            try
            {
                System.Net.NetworkInformation.PingReply reply;
                try
                {
                    reply = p.Send(ipAddress);
                }
                catch (Exception　ex)
                {
                    setColorError();
                    Console.WriteLine("例外発生");
                    Console.WriteLine(ex.Message);
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine(ex.InnerException.Message);
                    }
                    Console.WriteLine("ホスト名 OR IPアドレス："　+ ipAddress);
                    Console.Read();

                    return;
                }
                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                {
                    setColorSuccess();
                    Console.WriteLine("ping正常終了");
                    Console.WriteLine("サーバーは起動しています。");
                    System.Threading.Thread.Sleep(2000);
                    return;
                }
                else
                {
                    setColorError();
                    Console.WriteLine("ping失敗");
                }

            }
            finally
            {
                p.Dispose();
                Console.ResetColor();
            }
            Console.WriteLine("マジックパケットを送信します。");
            SendMagicPacket(physicalAddress);
            System.Threading.Thread.Sleep(2000);

        }

        /// <summary>
        /// コンソールの文字色をエラーにします。
        /// </summary>
        static private void setColorError()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.BackgroundColor = ConsoleColor.DarkRed;
        }

        /// <summary>
        /// コンソールの文字色を成功にします。
        /// </summary>
        static private void setColorSuccess()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
        }

        /// <summary>
        /// マジックパケットを送信します。
        /// </summary>
        /// <param name="address">IPアドレス</param>
        /// <param name="subnetmask">サブネットマスク</param>
        /// <param name="physicalAddress">起動するマシンのMACアドレス</param>
        static private void SendMagicPacket(string address, string subnetmask, byte[] physicalAddress)
        {
            SendMagicPacket(IPAddress.Parse(address), IPAddress.Parse(subnetmask), physicalAddress);
        }

        /// <summary>
        /// マジックパケットを送信します。
        /// </summary>
        /// <param name="address">IPアドレス</param>
        /// <param name="subnetmask">サブネットマスク</param>
        /// <param name="physicalAddress">起動するマシンのMACアドレス</param>
        static private void SendMagicPacket(IPAddress address, IPAddress subnetmask, byte[] physicalAddress)
        {
            uint uip = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
            uint usub = BitConverter.ToUInt32(subnetmask.GetAddressBytes(), 0);

            uint result = uip | (usub ^ 0xFFFFFFFF);

            SendMagicPacket(new IPAddress(result), physicalAddress);
        }

        /// <summary>
        /// ブロードキャストアドレス（255.255.255.255）に対してマジックパケットを送信します。
        /// </summary>
        /// <param name="physicalAddress">起動するマシンのMACアドレス</param>
        static private void SendMagicPacket(byte[] physicalAddress)
        {
            SendMagicPacket(IPAddress.Broadcast, physicalAddress);
        }

        /// <summary>
        /// 指定されたアドレスに対してマジックパケットを送信します。
        /// 送信先のアドレスはブロードキャストアドレスである必要があります。
        /// </summary>
        /// <param name="broad">ブロードキャストアドレス</param>
        /// <param name="physicalAddress">起動するマシンのMACアドレス</param>
        static private void SendMagicPacket(string broad, byte[] physicalAddress)
        {
            SendMagicPacket(IPAddress.Parse(broad), physicalAddress);
        }

        /// <summary>
        /// 指定されたアドレスに対してマジックパケットを送信します。
        /// 送信先のアドレスはブロードキャストアドレスである必要があります。
        /// </summary>
        /// <param name="broad">ブロードキャストアドレス</param>
        /// <param name="physicalAddress">起動するマシンのMACアドレス</param>
        static private void SendMagicPacket(IPAddress broad, byte[] physicalAddress)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            for (int i = 0; i < 6; i++)
            {
                writer.Write((byte)0xff);
            }
            for (int i = 0; i < 16; i++)
            {
                writer.Write(physicalAddress);
            }

            UdpClient client = new UdpClient();
            client.EnableBroadcast = true;
            client.Send(stream.ToArray(), (int)stream.Position, new IPEndPoint(broad, 0));
        }
    }
}
