using System;
using System.Collections.Generic;
using System.Text;

namespace WorkFlowGenerator.Services;

public interface IProjectDiscoveryService
{
    string DiscoverProject(string path);
}
