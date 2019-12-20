﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Hummingbird.Extersions.Resilience.Http
{
    public class HttpUrlResolver : IHttpUrlResolver
    {
        private readonly Hummingbird.DynamicRoute.IServiceLocator _serviceLocator;
        private readonly Hummingbird.LoadBalancers.DefaultLoadBalancerFactory<Hummingbird.DynamicRoute.ServiceEndPoint> _balancerFactory;
        private readonly Hummingbird.LoadBalancers.ILoadBalancer<Hummingbird.DynamicRoute.ServiceEndPoint> _loadBalancer;

        public HttpUrlResolver(
            Hummingbird.DynamicRoute.IServiceLocator serviceLocator)
        {
            this._serviceLocator = serviceLocator;
            this._balancerFactory = new Hummingbird.LoadBalancers.DefaultLoadBalancerFactory<Hummingbird.DynamicRoute.ServiceEndPoint>();
            this._loadBalancer = _balancerFactory.Get(() => new List<Hummingbird.DynamicRoute.ServiceEndPoint>());
        }

        public async Task<string> Resolve(string value)
        {
            if (_serviceLocator != null)
            {

                var result = value;
                var paramList = GetParameters(result);
                foreach (var param in paramList)
                {
                    if (!string.IsNullOrEmpty(param))
                    {
                        var args = param.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                        var serviceName = args[0];
                        var tagFilter = "";
                        if (args.Length == 2)
                        {
                            tagFilter = args[1];
                        }

                        //获取服务地址
                        var endPoints = await _serviceLocator.GetAsync(serviceName, tagFilter);
                        //获取一个地址
                        var targetEndPoint = _loadBalancer.Lease(endPoints.ToList());

                        result = result.Replace("{" + param + "}", $"{targetEndPoint.Address}:{targetEndPoint.Port}");
                        break;
                    }
                }
                return result;
            }
            else
            {
                return value;
            }
        }

        private List<string> GetParameters(string text)
        {
            var matchVale = new List<string>();
            string Reg = @"(?<=\{)[^\${}]*?(?=})";
            string key = string.Empty;
            foreach (Match m in Regex.Matches(text, Reg))
            {
                matchVale.Add(m.Value.TrimEnd('|'));
            }
            return matchVale;
        }
    }
}