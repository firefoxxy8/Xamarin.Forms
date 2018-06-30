using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;
using System.Linq;

#if UITEST
using Xamarin.UITest;
using NUnit.Framework;
using Xamarin.Forms.Core.UITests;
#endif

namespace Xamarin.Forms.Controls.Issues
{

#if UITEST
	[NUnit.Framework.Category(UITestCategories.LifeCycle)]
	[NUnit.Framework.Category(UITestCategories.Navigation)]
#endif
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 2338, "Change Main Page In Constructor", issueTestNumber: 1)]
	public class Issue2338_Ctor : TestNavigationPage
	{
		public Issue2338_Ctor()
		{
		}

		protected override void Init()
		{
		}

		protected override void OnAppearing()
		{
			Navigation.PushAsync(new InternalPage());
		}

		[Preserve(AllMembers = true)]
		public class InternalPage : ContentPage
		{
			public InternalPage()
			{
				Application.Current.MainPage = Issue2338TestHelper.CreateSuccessPage(nameof(Issue2338_Ctor));
			}
		}

#if UITEST && !__WINDOWS__ && !__IOS__
		[Test]
		public async Task SwapPagesOut_Ctor()
		{
			await Issue2338TestHelper.TestForSuccess(RunningApp, nameof(Issue2338_Ctor));
		}
#endif
	}

#if UITEST
	[NUnit.Framework.Category(UITestCategories.LifeCycle)]
	[NUnit.Framework.Category(UITestCategories.Navigation)]
#endif
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 2338, "Change Main Page In Constructor Part 2", issueTestNumber: 2)]
	public class Issue2338_Ctor_MultipleChanges : TestNavigationPage
	{
		public Issue2338_Ctor_MultipleChanges()
		{
		}

		protected override void Init()
		{
			PushAsync(new ContentPage());
		}

		protected override void OnAppearing()
		{
			Navigation.PushAsync(new InternalPage(0));
		}

		[Preserve(AllMembers = true)]
		public class InternalPage : ContentPage
		{
			private readonly int _permutations;
			public InternalPage(int permutations)
			{
				_permutations = permutations;
				if (permutations > 5)
				{
					Device.BeginInvokeOnMainThread(() =>
					{
						Application.Current.MainPage = Issue2338TestHelper.CreateSuccessPage(nameof(Issue2338_Ctor_MultipleChanges));
					});
				}
				else
				{
					Device.BeginInvokeOnMainThread(() =>
					{

						Application.Current.MainPage =
							new NavigationPage(new InternalPage(permutations + 1) { Title = "Title 1" });
					});
				}
			}

			protected override void OnAppearing() => Debug.WriteLine($"OnAppearing: {_permutations}");
			protected override void OnDisappearing() => Debug.WriteLine($"OnDisappearing: {_permutations}");
		}

#if UITEST
		[Test]
		public async Task SwapPagesOut_Ctor_MultipleChanges()
		{
			await Issue2338TestHelper.TestForSuccess(RunningApp, nameof(Issue2338_Ctor_MultipleChanges));
		}
#endif

	}

#if UITEST
	[NUnit.Framework.Category(UITestCategories.LifeCycle)]
	[NUnit.Framework.Category(UITestCategories.Navigation)]
#endif
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 2338, "Change Main Page In OnAppearing With Additional Changes", issueTestNumber: 3)]
	public class Issue2338_SwapMainPageDuringAppearing : TestNavigationPage
	{
		protected override void Init()
		{
			PushAsync(new InternalPage(10));
			PushAsync(new InternalPage(20));
			PushAsync(new InternalPage(30));

			var otherPage = new InternalPage(40);
			PushAsync(otherPage);

			otherPage.Appearing += async (object sender, EventArgs e) =>
			{
				await Task.Delay(1000);
				Application.Current.MainPage = new InternalTabbedPage(this);

				// this is here just to mimic the issue the user reported
				// where additional behavior was occuring during the bindingcontext change
				Application.Current.MainPage.BindingContext = new object();
			};

		}
		protected override void OnAppearing() => Debug.WriteLine($"OnAppearing: Issue2338");
		protected override void OnDisappearing() => Debug.WriteLine($"OnDisappearing: Issue2338");

		[Preserve(AllMembers = true)]
		public class InternalTabbedPage : TabbedPage
		{
			private readonly NavigationPage _navigationPage;
			public InternalTabbedPage(NavigationPage navigationPage)
			{
				_navigationPage = navigationPage;
				Children.Add(Issue2338TestHelper.CreateSuccessPage(nameof(Issue2338_SwapMainPageDuringAppearing)));
			}

			protected override void OnBindingContextChanged()
			{
				_navigationPage.PushAsync(new InternalPage(50));
				base.OnBindingContextChanged();
				_navigationPage.PushAsync(new InternalPage(60));
			}


			protected override void OnAppearing() => Debug.WriteLine($"OnAppearing: InternalTabbedPage");
			protected override void OnDisappearing() => Debug.WriteLine($"OnDisappearing: InternalTabbedPage");
		}

		[Preserve(AllMembers = true)]
		class InternalPage : ContentPage
		{
			private int v;

			public InternalPage(int v)
			{
				this.v = v;
				this.Content = new Label { Text = v.ToString() };
			}

			protected override void OnAppearing() => Debug.WriteLine($"OnAppearing: {v}");
			protected override void OnDisappearing() => Debug.WriteLine($"OnDisappearing: {v}");
		}

#if UITEST
		[Test]
		public async Task SwapPagesOut_SwapMainPageDuringAppearing()
		{
			await Issue2338TestHelper.TestForSuccess(RunningApp, nameof(Issue2338_SwapMainPageDuringAppearing));
		}
#endif
	}

#if UITEST
	[NUnit.Framework.Category(UITestCategories.LifeCycle)]
	[NUnit.Framework.Category(UITestCategories.Navigation)]
#endif
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 2338, "Change Main Page Tabbed Page", issueTestNumber: 4)]
	public class Issue2338_TabbedPage : TestTabbedPage
	{
		public Issue2338_TabbedPage() : base()
		{
		}


		protected override void Init()
		{
			Children.Add(new ContentPage());
			Children.Add(new ContentPage());
			Children.Add(new ContentPage());
			Children.Add(new InternalPage(this));
		}
		protected override void OnAppearing()
		{
			base.OnAppearing();
			SelectedItem = Children.Last();
		}


		[Preserve(AllMembers = true)]
		class InternalPage : ContentPage
		{
			private readonly TabbedPage _tabbedPage;

			public InternalPage(TabbedPage tabbedPage)
			{
				_tabbedPage = tabbedPage;
			}

			protected override void OnAppearing()
			{
				base.OnAppearing();
				_tabbedPage.Children.Add(new ContentPage());

				Application.Current.MainPage = Issue2338TestHelper.CreateSuccessPage(nameof(Issue2338_TabbedPage));
				_tabbedPage.Children.Add(new ContentPage());
			}
		}


#if UITEST && !__WINDOWS__
		[Test]
		public async Task SwapPagesOut_TabbedPage()
		{
			await Issue2338TestHelper.TestForSuccess(RunningApp, nameof(Issue2338_TabbedPage));
		}
#endif
	}



#if UITEST
	[NUnit.Framework.Category(UITestCategories.LifeCycle)]
	[NUnit.Framework.Category(UITestCategories.Navigation)]
#endif
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 2338, "Change Master Details Pages", issueTestNumber: 5)]
	public class Issue2338_MasterDetailsPage : TestContentPage
	{

		protected override void Init()
		{
		}

		protected override void OnAppearing()
		{
			Application.Current.MainPage = new InternalMasterDetailsPage();
		}

		[Preserve(AllMembers = true)]
		class InternalMasterDetailsPage : MasterDetailPage
		{
			public InternalMasterDetailsPage()
			{
				Detail = new NavigationPage(new ContentPage() { Title = "Details" });
				Master = new ContentPage() { Title = "Master" };
			}

			protected override async void OnAppearing()
			{
				base.OnAppearing();
				await Task.Delay(500);
				Detail.Navigation.PushAsync(new ContentPage());
				Detail.Navigation.PushModalAsync(new NavigationPage(new ContentPage() { Title = "Details 2", Content = new Label() { Text = "Fail Still Modal" } }));

				var navPage = new NavigationPage(new ContentPage() { Title = "Details" });
				Detail = navPage;
				Application.Current.MainPage = Issue2338TestHelper.CreateSuccessPage(nameof(Issue2338_MasterDetailsPage));
				navPage.PushAsync(new ContentPage() { Title = "Details 2", Content = new Label(){ Text = "Fail Second Pushed Page"} });
			}
		}

#if UITEST && !__IOS__
		[Test]
		public async Task SwapPagesOut_MasterDetailsPage()
		{
			await Issue2338TestHelper.TestForSuccess(RunningApp, nameof(Issue2338_MasterDetailsPage));
		}
#endif
	}





	public static class Issue2338TestHelper
	{
		public static Page CreateSuccessPage(string name)
		{
			return new NavigationPage(new ContentPage() { Title = "Title 1", Content = new Label() { Text = $"Success: {name.Replace("_", " ")}" } });
		}

#if UITEST
		public static async Task TestForSuccess(IApp RunningApp, string name)
		{
			//It takes a second for everything to settle
			await Task.Delay(5000);
			RunningApp.WaitForElement($"Success: {name.Replace("_", " ")}");
		}
#endif
	}
}
