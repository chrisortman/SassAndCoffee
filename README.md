# SassAndCoffee

This library adds simple, drop-in support for Sass/SCSS as well as Coffeescript.
Javascript and Coffeescript files can also be Minified and combined via UglifyJS.

How to use:

* Add the package reference via NuGet
* Add a .coffee, .scss, or .sass file to your project (an easy test is to just
  rename a CSS file to .scss)
* Reference the file as if it was a CSS file (i.e. to reference
  "scripts/test.coffee", you should reference "scripts/test.js" in your SCRIPT
  tag)
* To get the minified version of a script, reference the file as ".min.js" (i.e.
  "~/Scripts/MyCoolFile.min.js")

That's all there is to it! Files will be cached in your AppData folder and will
be regenerated whenever you modify them.

# How does it work?

SassAndCoffee embeds the original compilers in the DLL (Sass 3.2.0 and CoffeeScript 1.1.0
as of this writing) and uses IronRuby and Jurassic respectively to execute the
compilers against your source.

# Combining files

To combine a group of Javascript files together into one file, create a file in
your scripts folder with an extension of ".combine" - in this file, list the
files you want to combine (either as paths relative to the same folder, or as
full paths via '~'). Then, reference the file as if it were a Javascript file
(i.e. "all\_debug.combine" => "all\_debug.js")

# Why is this better than [SOMEOTHERPROJECT]

* No external processes are executed
* You don't have to install Ruby or node.js
* It's in NuGet so you don't have to fiddle with web.config
* Files are cached and are rebuilt as-needed.


# Problems

If you run into bugs / have feature suggestions / have questions, please either send me an Email at paul@paulbetts.org, or file a Github bug. 


# Thanks to:

Several folks helped me out with some of the integration details of this project
- if it weren't for them, I would still be stuck in the mud right now:

* David Padbury for helping me out with the CoffeeScript compiler
* Levi Broderick for giving me a few hints as to how to rig up the HttpModule
* Jimmy Schementi for telling me the proper way to redirect 'requires' to an embedded resource
* Thanks to Hampton Catlin and Jeremy Ashkenas for creating such awesome languages in the first place
