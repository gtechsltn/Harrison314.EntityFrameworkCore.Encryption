﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harrison314.EntityFrameworkCore.Encryption.CryptoProviders.Remote
{
    internal class FilterAcceptKeyIdsResponse
    {
        public string SelectedKeyId
        {
            get;
            set;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public FilterAcceptKeyIdsResponse()
        {

        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
