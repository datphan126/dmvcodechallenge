using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DmvAppointmentScheduler
{
    class Program
    {
        public static Random random = new Random();
        public static List<Appointment> appointmentList = new List<Appointment>();
        static void Main(string[] args)
        {
            CustomerList customers = ReadCustomerData();
            TellerList tellers = ReadTellerData();
            Calculation(customers, tellers);
            OutputTotalLengthToConsole();

        }
        private static CustomerList ReadCustomerData()
        {
            string fileName = "CustomerData.json";
            string path = Path.Combine(Environment.CurrentDirectory, @"InputData\", fileName);
            string jsonString = File.ReadAllText(path);
            CustomerList customerData = JsonConvert.DeserializeObject<CustomerList>(jsonString);
            return customerData;

        }
        private static TellerList ReadTellerData()
        {
            string fileName = "TellerData.json";
            string path = Path.Combine(Environment.CurrentDirectory, @"InputData\", fileName);
            string jsonString = File.ReadAllText(path);
            TellerList tellerData = JsonConvert.DeserializeObject<TellerList>(jsonString);
            return tellerData;

        }
        static void Calculation(CustomerList customers, TellerList tellers)
        {
            Dictionary<string, List<Customer>> customerDict = new Dictionary<string, List<Customer>>();
            Dictionary<string, List<Teller>> tellerDict = new Dictionary<string, List<Teller>>();
            
            // Add customers to the customerDict by type
            foreach(Customer customer in customers.Customer)
            {
                // If the dictionary doesn't have the type, create one
                if (!customerDict.ContainsKey(customer.type))
                {
                    customerDict.Add(customer.type, new List<Customer>());
                }
                customerDict[customer.type].Add(customer);
            }

            // Add tellers to the tellerDict by specialty type
            foreach (Teller teller in tellers.Teller)
            {
                // If the dictionary doesn't have the type, create one
                if (!tellerDict.ContainsKey(teller.specialtyType))
                {
                    tellerDict.Add(teller.specialtyType, new List<Teller>());
                }
                tellerDict[teller.specialtyType].Add(teller);
            }

            double totalDuration = 0;
            Dictionary<string, double> durationDict = new Dictionary<string, double>();
            Dictionary<string, List<Customer>> temporaryCustomerDict = new Dictionary<string, List<Customer>>(customerDict);
            // Process customers that have the service type matching with one of specialty types
            foreach (string type in customerDict.Keys)
            {
                totalDuration = 0;
                if (tellerDict.ContainsKey(type)){
                    // Find the number of customers per teller
                    double customerListLen = customerDict[type].Count;
                    double tellerListLen = tellerDict[type].Count;
                    int customersPerTeller = Convert.ToInt32(Math.Ceiling(customerListLen / tellerListLen));

                    // We do the following sorting so that customers that have long durations will be assigned to tellers that have small multipliers
                    // Sort the customer list in descending order by duration
                    customerDict[type].Sort((a, b) => Convert.ToDouble(b.duration).CompareTo(Convert.ToDouble(a.duration)));
                    // Sort the teller list in ascending order by multiplier
                    tellerDict[type].Sort((a, b) => Convert.ToDouble(a.multiplier).CompareTo(Convert.ToDouble(b.multiplier)));

                    int count = 1;
                    int tellerIndex = 0;
                    // Appointment creation
                    foreach(Customer customer in customerDict[type])
                    {
                        // Create a new appointment
                        var appointment = new Appointment(customer, tellerDict[type][tellerIndex]);
                        appointmentList.Add(appointment);
                        // Calculate the total duration for this specialty type
                        double tellerMultiplier = Convert.ToDouble(tellerDict[type][tellerIndex].multiplier);
                        double customerDuration = Convert.ToDouble(customer.duration);
                        totalDuration += tellerMultiplier * customerDuration;
                        // Get the next teller
                        if (count == customersPerTeller)
                        {
                            count = 1;
                            tellerIndex++;
                        }
                        else
                        {
                            count++;
                        }
                    }
                    // Remove assigned customers
                    temporaryCustomerDict.Remove(type);
                    // Update the total duration for this type
                    durationDict.Add(type, totalDuration);
                }
            }
            // Update the dictionary to remove customers that have been assigned
            customerDict = new Dictionary<string, List<Customer>>(temporaryCustomerDict);

            // Process customers that do not have a matching type
            foreach (string customerType in customerDict.Keys)
            {
                // Customers will be assigned to the teller group which has the shortest appointment duration
                // We first sort the list by duration in ascending order and then we get the first element which is the shortest duration
                var durationList = durationDict.ToList();
                durationList.Sort((a, b) => a.Value.CompareTo(b.Value));
                string tellerType = durationList[0].Key;
                double shortestDuration = durationList[0].Value;

                // Find the number of customers per teller
                double customerListLen = customerDict[customerType].Count;
                double tellerListLen = tellerDict[tellerType].Count;
                int customersPerTeller = Convert.ToInt32(Math.Ceiling(customerListLen / tellerListLen));

                int count = 1;
                int tellerIndex = 0;
                // Appointment creation
                foreach (Customer customer in customerDict[customerType])
                {
                    // Create a new appointment
                    var appointment = new Appointment(customer, tellerDict[tellerType][tellerIndex]);
                    appointmentList.Add(appointment);
                    // Calculate the total duration for this specialty type
                    shortestDuration += Convert.ToDouble(customer.duration);
                    // Get the next teller
                    if (count == customersPerTeller)
                    {
                        count = 1;
                        tellerIndex++;
                    }
                    else
                    {
                        count++;
                    }
                }
                // Update total duration of this specialty type group
                durationDict[tellerType] = shortestDuration;
            }
        }
        static void OutputTotalLengthToConsole()
        {
            var tellerAppointments =
                from appointment in appointmentList
                group appointment by appointment.teller into tellerGroup
                select new
                {
                    teller = tellerGroup.Key,
                    totalDuration = tellerGroup.Sum(x => x.duration),
                };
            var max = tellerAppointments.OrderBy(i => i.totalDuration).LastOrDefault();
            Console.WriteLine("Teller " + max.teller.id + " will work for " + max.totalDuration + " minutes!");
        }

    }
}
