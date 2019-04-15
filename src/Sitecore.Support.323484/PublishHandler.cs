
namespace Sitecore.Support.Publishing.WebDeploy
{
    using Sitecore.Diagnostics;
    using Sitecore.Events;
    using Sitecore.Publishing;
    using Sitecore.Publishing.WebDeploy;
    using Sitecore.Publishing.WebDeploy.Decorators;
    using Sitecore.Publishing.WebDeploy.Sites;
    using Sitecore.StringExtensions;
    using Sitecore.Threading;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class PublishHandler
    {
        private List<Task> _tasks = new List<Task>();

        public new void AddTask(Task task)
        {
            this._tasks.Add(task);
        }

        private DeploymentTaskRunner GetRunner(Task task)
        {
            DeploymentTaskRunner runner = new DeploymentTaskRunner();
            runner.SourceSite = (task.LocalRoot == null) ? new LocalApplicationSite() : new FolderDeploymentSite(task.LocalRoot);
            runner.TargetSite = new FolderDeploymentSite(task.RemoteRoot);

            if (task.TargetServer != null)
            {
                
                RemoteDecorator item = new RemoteDecorator
                {
                    ComputerName = task.TargetServer
                };

                if (!task.UserName.IsNullOrEmpty())
                {
                    item.UserName = task.UserName;
                    item.Password = task.Password;
                }
                else
                {
                    Sitecore.Abstractions.SettingsWrapper setWDCred = new Abstractions.SettingsWrapper();
                    item.UserName = setWDCred.GetAppSetting("WebDeploy.UserName");
                    item.Password = setWDCred.GetAppSetting("WebDeploy.Password");
                }
                runner.TargetSite.Decorators.Add(item);
            }

            runner.SyncOptions.DoNotDelete = false;
            runner.SyncOptions.UseChecksum = true;
            runner.SourceSite.Decorators.Add(new TraceDecorator(delegate (string level, string message, object data)
            {
                object[] parameters = new object[] { level, message };
                Log.Info("WebDeploy {0} : {1}".FormatWith(parameters), this);
            }));
            runner.TargetSite.Decorators.Add(new TraceDecorator(delegate (string level, string message, object data)
            {
                object[] parameters = new object[] { level, message };
                Log.Info("WebDeploy {0} : {1}".FormatWith(parameters), this);
            }));

            task.Paths.Apply<string>(delegate (string path)
            {
                runner.Paths.Add(path);
            });
            return runner;
        }



        public new void OnPublish(object sender, EventArgs args)
        {
            WaitCallback callback = null;
            Publisher publisher = SitecoreEventArgs.GetObject(args, 0) as Publisher;
            if (publisher != null)
            {
                if (this.Synchronous)
                {
                    this.Run(publisher);
                }
                else
                {
                    if (callback == null)
                    {
                        callback = delegate (object o)
                        {
                            lock (this)
                            {
                                this.Run(publisher);
                            }
                        };
                    }
                    ManagedThreadPool.QueueUserWorkItem(callback);
                }
            }
        }

        private void Run(Publisher publisher)
        {
            foreach (Task task in this._tasks)
            {
                if ((task.TargetDatabase == null) || (publisher.Options.TargetDatabase.Name == task.TargetDatabase))
                {
                    try
                    {
                        this.GetRunner(task).Execute();
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception.Message, exception, this);
                    }
                }
            }
        }

        public bool Synchronous { get; set; }
    }
}
