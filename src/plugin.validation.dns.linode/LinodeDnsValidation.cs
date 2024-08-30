﻿using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Plugins.ValidationPlugins.Linode;
using PKISharp.WACS.Services;
using System.Collections.Concurrent;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [IPlugin.Plugin1<
        LinodeOptions, LinodeOptionsFactory,
        DnsValidationCapability, LinodeJson, LinodeArguments>
        ("12fdc54c-be30-4458-8066-2c1c565fe2d9",
        "Linode", "Create verification records in Linode DNS")]
    internal class LinodeDnsValidation(
        LookupClientProvider dnsClient,
        ILogService logService,
        ISettingsService settings,
        DomainParseService domainParser,
        LinodeOptions options,
        SecretServiceManager ssm,
        IProxyService proxyService) : DnsValidation<LinodeDnsValidation>(dnsClient, logService, settings)
    {
        private readonly DnsManagementClient _client = new(ssm.EvaluateSecret(options.ApiToken) ?? "", logService, proxyService);
        private readonly Dictionary<string, int> _domainIds = [];
        private readonly ConcurrentDictionary<int, List<int>> _recordIds = new();

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var domain = domainParser.GetRegisterableDomain(record.Authority.Domain);
                var domainId = await _client.GetDomainId(domain);
                if (domainId == 0)
                {
                    throw new InvalidDataException("Linode did not return a valid domain id.");
                }
                _ = _domainIds.TryAdd(record.Authority.Domain, domainId);

                var recordName = RelativeRecordName(domain, record.Authority.Domain);
                var recordId = await _client.CreateRecord(domainId, recordName, record.Value);
                if (recordId == 0)
                {
                    throw new InvalidDataException("Linode did not return a valid domain record id.");
                }
                _ = _recordIds.AddOrUpdate(
                    domainId,
                    [recordId], 
                    (b, s) => [.. s, recordId]);
                return true;
            }
            catch (Exception ex)
            {
                _log.Warning(ex, $"Unable to create record at Linode");
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            if (_domainIds.TryGetValue(record.Authority.Domain, out var domainId))
            {
                if (_recordIds.TryGetValue(domainId, out var recordIds))
                {
                    foreach (var recordId in recordIds)
                    {
                        try
                        {
                            _ = await _client.DeleteRecord(domainId, recordId);
                        }
                        catch (Exception ex)
                        {
                            _log.Warning(ex, "Unable to delete record {recordId} from Linode domain {domainId}", recordId, domainId);
                        }
                    }
                }
            }
        }

    }
}
