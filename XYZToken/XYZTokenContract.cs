using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using Neo.SmartContract.Framework.Native;

namespace XYZToken
{
	[DisplayName("BhaskarDutta2209.XYZTokenContract")]
	[ManifestExtra("Author", "Bhaskar Dutta")]
	[ManifestExtra("Email", "bhaskardutta2209@gmail.com")]
	[ManifestExtra("Description", "First try at NEP-17 token")]
	public class XYZTokenContract : SmartContract {

		const string MAP_NAME = "XYZTokenContract";

		static readonly ulong InitialSupply = 1_000_000; // Total supply of 1M

		public static BigInteger TotalSupply() => InitialSupply;

		public static string Symbol() => "XYZ";

		public static ulong Decimal() => 8;

		[DisplayName("Transfer")]
		public static event Action<UInt160, UInt160, BigInteger> OnTransfer;

		private static StorageMap Balances => new StorageMap(Storage.CurrentContext, MAP_NAME);

		private static BigInteger Get(UInt160 key) => (BigInteger) Balances.Get(key);
		private static void Put(UInt160 key, BigInteger value) => Balances.Put(key, value);

		private static void Increase(UInt160 key, BigInteger value) {
			Put(key, Get(key) + value);
		}

		private static void Reduce(UInt160 key, BigInteger value) {
			var oldValue = Get(key);
			if(oldValue == value) {
				Balances.Delete(key);
			} else {
				Put(key, oldValue - value);
			}
		}

		public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount, object data) {
			
			if(!from.IsValid || !to.IsValid) {
				throw new Exception("The parameters from and to shouble be 20-bytes address");
			}

			if(amount < 0) {
				throw new Exception("The amount parameter must be greater than or equal to zero");
			}

			if(!from.Equals(Runtime.CallingScriptHash) && !Runtime.CheckWitness(from)) {
				throw new Exception("No Authorization");
			}

			if(Get(from) < amount) {
				throw new Exception("Insufficient balance");
			}

			Reduce(from, amount);
			Increase(to, amount);
			OnTransfer(from, to, amount);

			if(ContractManagement.GetContract(to) != null) {
				Contract.Call(to, "onPayment", CallFlags.None, new object[] {from, amount, data});
			}

			return true;
		}

		public static BigInteger BalanceOf(UInt160 account) {
			return Get(account);
		}

		[DisplayName("_deploy")]
		public static void Deploy(object data, bool update) {
			if(!update) {
				var tx = (Transaction) Runtime.ScriptContainer;
				var owner = (Neo.UInt160) tx.Sender;
				Increase(owner, InitialSupply);
				OnTransfer(null, owner, InitialSupply);
			}
		}

	}
}
