module GoModule2

go 1.21

require github.com/dafny-lang/DafnyRuntimeGo v0.0.0-20231204230030-1d44519b5706
        
require GoModule1 v0.0.0

replace github.com/dafny-lang/DafnyRuntimeGo v0.0.0-20231204230030-1d44519b5706 => ../DafnyRuntimeGo
replace GoModule1 v0.0.0 => ./DafnyModule1