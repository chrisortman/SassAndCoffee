require 'compass'
require 'compass/exec'

TOPLEVEL_BINDING = binding unless defined?(TOPLEVEL_BINDING)

def repl(scope = TOPLEVEL_BINDING)
  Repl.start(scope)
end

module Repl
  def self.start(scope = TOPLEVEL_BINDING)
    quitstr = ['quit', 'exit', '']
    while true
      stack = eval("caller[3..-1]", scope)
      print "\n#{stack.first}\n" if stack and not stack.empty?
      print 'rb> '
      input = gets.strip rescue 'quit'
      break if quitstr.include?(input)
      puts "=> #{
        begin
          eval(input, scope).inspect
        rescue LoadError => le
          puts le.inspect
        rescue SyntaxError => se
          puts se.inspect
        rescue => e
          puts e.inspect
        end
      }"
    end
  end
end

$load_paths = ['e:\\code\\sassandcoffee\\webtest\\app_compass\\blueprint\\stylesheets','e:\\code\\sassandcoffee\\webtest\\app_compass\\compass\\stylesheets']

$load_paths2 = ['e:/code/sassandcoffee/webtest/app_compass/blueprint/stylesheets','e:/code/sassandcoffee/webtest/app_compass/compass/stylesheets']
$sass_option = {:syntax => :sass, :load_paths => $load_paths2 }
$scss_option = {:syntax => :scss, :load_paths => $load_paths2 }

file_text = System::IO::File.ReadAllText("e:\\code\\sassandcoffee\\webtest\\content\\sass\\screen.scss")

System::Diagnostics::Debugger.launch

puts Sass.compile(file_text,$scss_option)
