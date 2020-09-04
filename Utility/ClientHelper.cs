using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ColecticaSdkMvc.Models;
using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using System.Configuration;

namespace ColecticaSdkMvc.Utility
{
	public static class ClientHelper
	{
       
        public static WcfRepositoryClient GetClient()
		{
            var Url = ConfigurationManager.AppSettings["URL"].ToString();
            var UserName = ConfigurationManager.AppSettings["UserName"].ToString();
            var UserPassword = System.Configuration.ConfigurationManager.AppSettings["Password"].ToString();

            RepositoryConnectionInfo info = new RepositoryConnectionInfo()
            {
                // TODO Update the hostname as appropriate.
                // Url = "localhost",
                //Url = "https://clsr-clrdp2w01p.ad.ucl.ac.uk",
                //AuthenticationMethod = RepositoryAuthenticationMethod.Windows,
                //TransportMethod = RepositoryTransportMethod.NetTcp

                Url = Url,
                AuthenticationMethod = RepositoryAuthenticationMethod.UserName,
                UserName = UserName,
                Password = UserPassword,     
                TransportMethod = RepositoryTransportMethod.NetTcp
            };
            
			var client = new WcfRepositoryClient(info);
			return client;
		}

    }
}