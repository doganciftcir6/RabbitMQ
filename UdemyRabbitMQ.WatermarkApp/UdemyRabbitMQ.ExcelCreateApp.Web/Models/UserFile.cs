using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace UdemyRabbitMQ.ExcelCreateApp.Web.Models
{
    public enum FileStatus
    {
        Creating,
        Completed
    }
    public class UserFile
    {
        public int Id { get; set; }
        // Identity UserId'yi String olarak tutacaktır
        public string UserId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime? CreatedDate { get; set; }
        public FileStatus FileStatus { get; set; }
        // Bu property'in veri tabanına herhangi bir tabloya maplanmesini istemiyorum
        [NotMapped]
        public string GetCreatedDate => CreatedDate.HasValue ? CreatedDate.Value.ToShortDateString() : "-";
    }
}
