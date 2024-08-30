﻿using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public sealed class Route53Arguments : BaseArguments
    {
        [CommandLine(Description = "AWS IAM role for the current EC2 instance to login into Amazon Route 53. Note that you should provide the IAM name instead of the ARN.")]
        public string? Route53IAMRole { get; set; }

        [CommandLine(Description = "Access key ID to login into Amazon Route 53.")]
        public string? Route53AccessKeyId { get; set; }

        [CommandLine(Description = "Secret access key to login into Amazon Route 53.", Secret = true)]
        public string? Route53SecretAccessKey { get; set; }
    }
}