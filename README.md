# Example CLI Usage 

Clone, build the repo, and run the CLI.

```bash

git clone git@github.com:1080network/dotnet.git
cd dotnet && dotnet build RealTimePayments.sln

# Example Invocation of the CLI
src/RealTimePaymentsCLI/bin/Debug/net8.0/RealTimePaymentsCLI /home/ddm/proj/certs/hron3n/rootca.crt  /home/ddm/proj/certs/hron3n/intermediate.crt /home/ddm/proj/certs/hron3n/externalclient-testcert0_hron3n_members_mica_io.pfx password https://api.hron3n.members.mica.io:443
{ "status": "STATUS_SUCCESS", "serverTime": "2024-05-07T16:23:46.509205631Z", "buildVersion": "go1.22.1 X:boringcrypto", "buildSha": "62e13c8e4310133587d370478199d595621480ae", "buildTime": "2024-04-10T21:11:10Z", "serviceType": "Partner", "serverStartTime": "2024-04-30T15:56:27.445310078Z" }
```
