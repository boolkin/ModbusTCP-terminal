using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net;

namespace ModBusTCP_terminal {
  class Program {

    static void Main(string[] args) {
      Console.WriteLine("Введите IP адрес ведомого устройства. Порт по умолчанию 502");
      string server = Console.ReadLine();
      int port = 502;

      while (true) {
        // Modbus TCP frame format = MBAP Header + PDU (Modbus Application Header + Protocol Data Unit)
        Console.WriteLine("Введите запрос для ведомого устройства в формате");
        Console.WriteLine("{MbAddr} {CmdCode} {AddrH} {AddrL} {QntH} {QntL}");
        string MBAP = "0001 0000 0006";
        // 0001 - Идентификатор транзакции	Transaction Identifier
        // 0000 - Идентификатор протокола	Protocol Identifier
        // 0006 - Длина (6 байтов идут следом)	Message Length
        string PDU = Console.ReadLine();
        // Считываем ввод PDU с клавиатуры. Формат такой же как ModBus RTU, но без контрольной суммы: 
        // {MbAddr} {CmdCode} {AddrH} {AddrL} {QntH} {QntL}
        // например - 01 03 00 01 00 02, где 01- адрес ведомого устройства
        // 03 - код команды чтение регистров хранения (Holding Register или Analog Output), 
        // 00 01 - адрес регистра с которого начинается чтение
        // 00 02 - количество читаемых регистров

        string SendToDevice = MBAP + PDU;
        Connect(server, port, SendToDevice.Replace(" ", ""));
      }
    }

    static void Connect(String server, int port, String message) {
      try {
        // Create a TcpClient.
        // Note, for this client to work you need to have a TcpServer
        // connected to the same address as specified by the server, port
        // combination.

        TcpClient client = new TcpClient(server, port);

        // Переводим полученное сообщение в массив байт
        Byte[] data = Enumerable.Range(0, message.Length)
          .Where(x => x % 2 == 0)
          .Select(x => Convert.ToByte(message.Substring(x, 2), 16))
          .ToArray();

        // Get a client stream for reading and writing.
  
        NetworkStream stream = client.GetStream();

        // Send the message to the connected TcpServer.
        stream.Write(data, 0, data.Length);

        Console.WriteLine("Sent: {0}", message);

        // Receive the TcpServer.response.

        // Buffer to store the response bytes.
		// Так как заголовок ответа содержит конкретное число байт, а число байт в ответе соотносится с тем что запрашивается, то можно попробовать динамически посчитать буфер, но при условии что в ответе будут приходить данные в формате int16 а не int32 или float
		// Для начала узнаем сколько регистров запрашивается. По структуре это у нас последняя цифра из полученных данных

		Console.WriteLine("Количество регистров запрошено: {0}", data[data.Length-1].ToString());
		// Длина заголовка обычно 9 байт + количество регистров * 2, т.к. int16 одно слово из двух байт
		int dataSize = data[data.Length-1] * 2;
		int arrSize = dataSize + 9;
		// создаем массив соответствующей длины 
        Byte[] dataReceived = new Byte[arrSize];
		
        // Read the first batch of the TcpServer response bytes.
        Int32 bytes = stream.Read(dataReceived, 0, dataReceived.Length);
		// Преобразуем byte array to hex string и выводим в консоль
        StringBuilder hex = new StringBuilder(dataReceived.Length * 2);
        foreach(byte b in dataReceived)
				hex.AppendFormat("{0:x2}", b);		
        Console.WriteLine("Received: {0}", hex.ToString());
		
		// парсим и выводим в консоль входящие значения регистров
		for (int i = 0; i < dataSize; i+=2) {
			Byte[] Register = new Byte[2];
			Array.Copy(dataReceived, 9+i, Register, 0, 2);
			Array.Reverse(Register);
			Console.WriteLine("Register {0} : {1}", i/2,BitConverter.ToInt16(Register, 0));
		}		
		

        // Close everything.
        //stream.Close();
        //client.Close();
      } catch (ArgumentNullException e) {
        Console.WriteLine("ArgumentNullException: {0}", e);
      } catch (SocketException e) {
        Console.WriteLine("SocketException: {0}", e);
      }

    }
  }
}