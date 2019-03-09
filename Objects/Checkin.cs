// using System;
// using System.Text;
// using System.Collections.Generic;

// namespace Marauder.Objects {

//   /// <summary>
//   /// TaskMessage is used for sending and recieving encrypted messages. 
//   /// 
//   /// When recieved from Core, the 'message' will be a base64 encoded, 
//   /// encrypted list of AgentTask objects (json formated)
//   /// 
//   /// When sent from the agent, the 'message' will be a base64 encoded,
//   /// encyrpted lists of TaskResponse objects (json formated).
//   /// </summary>
//   public class Checkin
//   {
//     public string AgentName;
//     public string IV;
//     public string HMAC;
//     public string Message;

//     public Checkin(Dictionary<string, string> TaskDictonary) {
//       AgentName = TaskDictonary["AgentName"];
//       IV = TaskDictonary["IV"];
//       HMAC = TaskDictonary["HMAC"];
//       Message = TaskDictonary["Message"];
//     }
//   }
// }