using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Experiments.Distances;
using KSDExperiments.FiniteContexts.Attributes;
using KSDExperiments.FiniteContexts.Features;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.FiniteContexts.PatternVector;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.FiniteContexts.Store;
using KSDExperiments.Pipelines;
using KSDExperiments.Reports;
using KSDExperiments.Util;


namespace KSDExperiments.FiniteContexts.Classifiers
{
    public abstract class Classifier
    {
        public User User { get; private set; }
        public FiniteContextsConfiguration Parameters { get; private set; }
        public DirectoryInfo TempFolder { get; private set; }

        public Classifier(User user, FiniteContextsConfiguration parameters, DirectoryInfo temp_folder)
        {
            User = user;
            Parameters = parameters;
            TempFolder = temp_folder;
        }

        public virtual void Initialize()
        {
        }

        public virtual void AppendTrainingSession(bool legitimate, Dictionary<string, double> method_values)
        {
        }

        public virtual void ClearClassifierCache() { }

        public abstract string Name { get; }

        public double FRR { get; protected set; }
        public double FAR { get; protected set; }

        public double ExpectedErrorRate { get; protected set; }

        public abstract void Retrain();
        public abstract bool IsLegitimate(Dictionary<string, double> numeric_attribute_values);
        public virtual bool[] AreLegitimate(Dictionary<string, double>[] numeric_attribute_values)
        {
            throw new NotImplementedException();
        }


        public delegate void OnMisclassifiedEvaluationSessionDelegate(Authentication authentication);
        public event OnMisclassifiedEvaluationSessionDelegate OnFalseRejection;
        protected void InvokeOnFalseRejection(Authentication authentication)
        {
            if (OnFalseRejection != null)
                OnFalseRejection(authentication);
        }

        public event OnMisclassifiedEvaluationSessionDelegate OnFalseAcceptance;
        protected void InvokeOnFalseAcceptance(Authentication authentication)
        {
            if (OnFalseAcceptance != null)
                OnFalseAcceptance(authentication);
        }

        public bool CanAuthenticate { get; protected set; }

        public virtual void AddEERs(Dictionary<string, ErrorMetrics> EERs)
        {
            if (EERs.ContainsKey(Name))
                EERs.Remove(Name);

            if (EERs.ContainsKey(Name + "_FAR"))
                EERs.Remove(Name + "_FAR");

            if (EERs.ContainsKey(Name + "_FRR"))
                EERs.Remove(Name + "_FRR");

            EERs.Add(Name, new ErrorMetrics(Name, 1.0 - ExpectedErrorRate / 100.0));
            EERs.Add(Name + "_FAR", new ErrorMetrics(Name, FAR / 100.0));
            EERs.Add(Name + "_FRR", new ErrorMetrics(Name, FRR / 100.0));
        }

        public virtual void SaveTraining(string path)
        {
            throw new NotImplementedException();
        }

        public virtual void SaveLastEvaluation(string path)
        {
            throw new NotImplementedException();
        }
    }
}
