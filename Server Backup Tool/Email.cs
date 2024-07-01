using System.Net.Mail;
using System.Net;
using System.Net.NetworkInformation;

namespace Server_Backup_Tool
{
    internal class Email
    {
        public static void StartEmail()
        {
            MailAddress fromAddress = new("hunternas2024@gmail.com", "Hunter NAS");
            MailAddressCollection toAddress = new()
            {
                new MailAddress("tobyhunter2000@gmail.com", "Toby"),
                new MailAddress("theningiachicken2@gmail.com", "Will")
            };

            SmtpClient smtp = new()
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, "dqcy dnry wlcv pptg")
            };

            string subject = "SBT Open Notification";
            string body = @"<html><body><p>Hello,</p>
<p>The Server Backup Tool has opened.</p>
<p>For any urgent issues, please message Toby on discord.</p>
<p>Thanks,</p>
<p>Hunter NAS</p>
<img src=""cid:footerImage"" width=""200"" height=""200""></body></html>";

            MailMessage message = new()
            {
                From = fromAddress,
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toAddress.ToString());

            Attachment footerImage = new(@"C:\Hunter Industries\Server Backup Tool\Content\HI Tech Logo.png")
            {
                ContentId = "footerImage"
            };
            footerImage.ContentDisposition.Inline = true;

            message.Attachments.Add(footerImage);
            smtp.Send(message);
        }

        public static void ConnectionEmail()
        {
            MailAddress fromAddress = new("hunternas2024@gmail.com", "Hunter NAS");
            MailAddressCollection toAddress = new()
            {
                new MailAddress("tobyhunter2000@gmail.com", "Toby"),
                new MailAddress("theningiachicken2@gmail.com", "Will")
            };

            SmtpClient smtp = new()
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, "dqcy dnry wlcv pptg")
            };

            string subject = "SBT Server Notification";
            string body = @"<html><body><p>Hello,</p>
<p>The server has failed to respond to a ping, please confirm if Hunter-Nas is active on Hamachi.</p>
<p>For any urgent issues, please message Toby on discord.</p>
<p>Thanks,</p>
<p>Hunter NAS</p>
<img src=""cid:footerImage"" width=""200"" height=""200""></body></html>";

            MailMessage message = new()
            {
                From = fromAddress,
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toAddress.ToString());

            Attachment footerImage = new(@"C:\Hunter Industries\Server Backup Tool\Content\HI Tech Logo.png")
            {
                ContentId = "footerImage"
            };
            footerImage.ContentDisposition.Inline = true;

            message.Attachments.Add(footerImage);
            smtp.Send(message);
        }

        public static void CloseEmail()
        {
            MailAddress fromAddress = new("hunternas2024@gmail.com", "Hunter NAS");
            MailAddressCollection toAddress = new()
            {
                new MailAddress("tobyhunter2000@gmail.com", "Toby"),
                new MailAddress("theningiachicken2@gmail.com", "Will")
            };

            SmtpClient smtp = new()
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, "dqcy dnry wlcv pptg")
            };

            string subject = "SBT Close Notification";
            string body = @"<html><body><p>Hello,</p>
<p>The Server Backup Tool has closed.</p>
<p>For any urgent issues, please message Toby on discord.</p>
<p>Thanks,</p>
<p>Hunter NAS</p>
<img src=""cid:footerImage"" width=""200"" height=""200""></body></html>";

            MailMessage message = new()
            {
                From = fromAddress,
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toAddress.ToString());

            Attachment footerImage = new(@"C:\Hunter Industries\Server Backup Tool\Content\HI Tech Logo.png")
            {
                ContentId = "footerImage"
            };
            footerImage.ContentDisposition.Inline = true;

            message.Attachments.Add(footerImage);
            smtp.Send(message);
        }
    }
}
