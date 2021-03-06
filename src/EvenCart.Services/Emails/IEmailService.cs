﻿#region License
// Copyright (c) Sojatia Infocrafts Private Limited.
// The following code is part of EvenCart eCommerce Software (https://evencart.co) Dual Licensed under the terms of
// 
// 1. GNU GPLv3 with additional terms (available to read at https://evencart.co/license)
// 2. EvenCart Proprietary License (available to read at https://evencart.co/license/commercial-license).
// 
// You can select one of the above two licenses according to your requirements. The usage of this code is
// subject to the terms of the license chosen by you.
#endregion

using System;
using System.Threading.Tasks;
using EvenCart.Core.Services;
using EvenCart.Data.Entity.Emails;

namespace EvenCart.Services.Emails
{
    public interface IEmailService : IFoundationEntityService<EmailMessage>
    {
        /// <summary>
        /// Sends an email with settings specified in the email info object and returns true if sending succeeds
        /// </summary>
        bool SendEmail(EmailMessage emailMessage, out Exception ex);

        /// <summary>
        /// Sends an email with settings specified in the email info object and returns true if sending succeeds
        /// </summary>
        bool SendEmail(EmailMessage emailMessage);

        /// <summary>
        /// Asynchronously sends an email with settings specified in the email info object and returns true if sending succeeds
        /// </summary>
        Task<bool> SendEmailAsync(EmailMessage emailMessage);

        void Queue(EmailMessage emailMessage);
    }
}