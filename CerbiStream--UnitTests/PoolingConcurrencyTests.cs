using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace CerbiStream.Tests
{
 public class PoolingConcurrencyTests
 {
 [Fact(DisplayName = "Dictionary pool - concurrent rent/return does not lose items")]
 public void DictionaryPool_ConcurrentRentReturn_NoLoss()
 {
 // Find adapter type
 var adapterType = AppDomain.CurrentDomain.GetAssemblies()
 .SelectMany(a =>
 {
 try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
 })
 .FirstOrDefault(t => t.Name == "GovernanceRuntimeAdapter");

 Assert.NotNull(adapterType);

 var rentMethod = adapterType.GetMethod("RentDictionary", BindingFlags.NonPublic | BindingFlags.Static);
 var returnMethod = adapterType.GetMethod("ReturnDictionaryToPool", BindingFlags.NonPublic | BindingFlags.Static);
 Assert.NotNull(rentMethod);
 Assert.NotNull(returnMethod);

 int concurrency =50;
 int iterationsPerTask =200;

 Parallel.For(0, concurrency, i =>
 {
 for (int j =0; j < iterationsPerTask; j++)
 {
 var d = (Dictionary<string, object>)rentMethod.Invoke(null, null)!;
 // use the dictionary
 d["task"] = i;
 // return
 returnMethod.Invoke(null, new object[] { d });
 }
 });

 // After operations, rent a few dictionaries and ensure they're cleared
 var rented = new List<Dictionary<string, object>>();
 for (int k =0; k <10; k++)
 {
 var d = (Dictionary<string, object>)rentMethod.Invoke(null, null)!;
 rented.Add(d);
 Assert.Empty(d);
 }

 // Return them
 foreach (var d in rented)
 returnMethod.Invoke(null, new object[] { d });
 }

 [Fact(DisplayName = "HashSet pool - concurrent rent/return does not lose items")]
 public void HashSetPool_ConcurrentRentReturn_NoLoss()
 {
 // Find adapter type
 var adapterType = AppDomain.CurrentDomain.GetAssemblies()
 .SelectMany(a =>
 {
 try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
 })
 .FirstOrDefault(t => t.Name == "GovernanceRuntimeAdapter");

 Assert.NotNull(adapterType);

 var rentMethod = adapterType.GetMethod("RentHashSet", BindingFlags.NonPublic | BindingFlags.Static);
 var returnMethod = adapterType.GetMethod("ReturnHashSet", BindingFlags.NonPublic | BindingFlags.Static);
 Assert.NotNull(rentMethod);
 Assert.NotNull(returnMethod);

 int concurrency =50;
 int iterationsPerTask =200;

 Parallel.For(0, concurrency, i =>
 {
 for (int j =0; j < iterationsPerTask; j++)
 {
 var s = (HashSet<string>)rentMethod.Invoke(null, null)!;
 s.Add($"t{ i }-{ j }");
 returnMethod.Invoke(null, new object[] { s });
 }
 });

 // After operations, rent a few hashsets and ensure they're cleared
 var rented = new List<HashSet<string>>();
 for (int k =0; k <10; k++)
 {
 var s = (HashSet<string>)rentMethod.Invoke(null, null)!;
 rented.Add(s);
 Assert.Empty(s);
 }

 foreach (var s in rented)
 returnMethod.Invoke(null, new object[] { s });
 }
 }
}
