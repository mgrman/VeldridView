using System;
using System.Reactive.Subjects;
using Xamarin.Forms;

namespace VeldridView
{
    public class VeldridViewPanel : View
    {
        public ISubject<IApplicationWindow?> Window { get; } = new BehaviorSubject<IApplicationWindow?>(null);
    }
}
